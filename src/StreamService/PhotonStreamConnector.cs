namespace Tool.Compet.Photon {
	using System;
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using MessagePack;

	/// Websocket: https://sookocheff.com/post/networking/how-do-websockets-work/
	public class PhotonStreamConnector : PhotonConnector {
		/// Be released when the client get disconnected.
		/// This will be passed to target hub at construct time.
		private object client;

		/// Raw communicator (send/receive) between server and the client.
		/// Be released when the client get disconnected.
		private WebSocket socket;

		public PhotonStreamConnector(object client, WebSocket socket) {
			this.client = client;
			this.socket = socket;

			// [Configure hub, connector and client]
			var hubList = new List<PhotonHub>();

			foreach (var (hubId, hubType) in PhotonServiceRegistry.AppHubServiceTypes()) {
				// Create new hubs for the client.
				var hub = Activator.CreateInstance(hubType, hubId, client, this) as PhotonHub;
				if (hub == null) {
					throw new Exception($"Could not create hub instance of: {hubType.Name}");
				}
				hubList.Add(hub);
			}

			// Register hubs
			var hubs = this.hubs;
			foreach (var hub in hubList) {
				hubs[hub.id] = hub;
			}
		}

		/// Send data to client.
		public async Task SendAsync(byte[] data) {
			await this.socket.SendAsync(
				buffer: new ArraySegment<byte>(data, 0, data.Length),
				messageType: WebSocketMessageType.Binary,
				endOfMessage: true,
				cancellationToken: CancellationToken.None
			);
		}

		/// Get data from the client.
		public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer) {
			return await this.socket.ReceiveAsync(
				buffer: buffer,
				cancellationToken: CancellationToken.None
			);
		}

		/// Close connection with the client.
		public async Task CloseAsync(string? message = null) {
			await socket.CloseAsync(
				closeStatus: WebSocketCloseStatus.NormalClosure,
				statusDescription: message,
				cancellationToken: CancellationToken.None
			);
		}

		/// Handle communication between client and server.
		/// In detail, this method will receive message from a client,
		/// and tell target hub sending the message to some clients.
		public async Task HandleCommunication(DkPhotonConnectionSetting? setting = null) {
			setting = setting ?? new();
			var readBuffer = new ArraySegment<byte>(new byte[setting.inBufferSize]);
			var inData = await socket.ReceiveAsync(readBuffer, CancellationToken.None);

			// This is called after handshaking completed.
			// Connection at this time is websocket, NOT http.
			// We just loop until the connection get disconnected from client or program exited.
			// while (webSocket.State == WebSocketState.Open) { }
			while (!inData.CloseStatus.HasValue) {
				switch (inData.MessageType) {
					case WebSocketMessageType.Binary: {
						/// Need branch to new method as MessagePack's requirement.
						ConsumeIncomingData(readBuffer.Array!, 0, inData.Count);
						break;
					}
					// case WebSocketMessageType.Text: {
					// 	var inMessage = Encoding.UTF8.GetString(buffer.Array!, 0, inResult.Count);
					// 	var messageToClient = Encoding.UTF8.GetBytes($"server[{this.GetHashCode()}]~ client-{userId} said: {inMessage}");
					// 	var messageToClientArr = new ArraySegment<byte>(messageToClient, 0, messageToClient.Length);
					// 	break;
					// }
					case WebSocketMessageType.Close: {
						await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
						break;
					}
					default: {
						Console.WriteLine($"Unhandled incomingResult.MessageType: {inData.MessageType}");
						break;
					}
				}

				// Read next incoming data
				inData = await socket.ReceiveAsync(readBuffer, CancellationToken.None);
			}
		}

		private void ConsumeIncomingData(byte[] buffer, int offset, int count) {
			// dkopt: we should avoid copying buffer?
			// https://github.com/neuecc/MessagePack-CSharp#be-careful-when-copying-buffers
			//
			// Format of inData:
			// Service: [messageType(byte), hubId(byte), methodId(short), parameters(msgPackObject)]
			// RPC    : [messageType(byte), hubId(byte), methodId(short), rpcTarget(byte), parameters(msgPackObject)]
			var inData = new byte[count];
			Array.Copy(buffer, offset, inData, 0, count);

			var reader = new MessagePackReader(inData);
			var arrLength = reader.ReadArrayHeader(); // MUST read header first, otherwise get error.

			var messageType = (DkPhotonMessageType)reader.ReadByte(); // total max 256 types
			var hubId = reader.ReadByte(); // total max 256 hubs/connection
			var methodId = reader.ReadInt16(); // max 64k methods/hub

			var hub = (IPhotonService)this.hubs[hubId];

			switch (messageType) {
				case DkPhotonMessageType.SERVICE: {
					var paramsOffset = (int)reader.Consumed;
					hub.HandleServiceRequest(methodId, inData, paramsOffset, count - paramsOffset);
					break;
				}
				case DkPhotonMessageType.RPC: {
					var rpcTarget = (DkPhotonRpcTarget)reader.ReadByte();
					hub.HandleRpcRequest(rpcTarget, inData);
					break;
				}
				default: {
					throw new Exception($"Invalid message type: {messageType}");
				}
			}
		}

		/// Called when the client disconnected from us.
		public void OnDisconnected() {
			this.client = null;
			this.socket = null;
		}
	}
}
