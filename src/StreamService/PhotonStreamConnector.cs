namespace Tool.Compet.Photon {
	using System;
	using System.Net.WebSockets;
	using MessagePack;

	/// Websocket: https://sookocheff.com/post/networking/how-do-websockets-work/
	public class PhotonStreamConnector : PhotonConnector {
		/// Raw communicator (send/receive) between server and the client.
		/// Be released when the client get disconnected.
		private WebSocket socket;

		/// Holds all termina-hubs for communication between server and the client.
		/// The first dimension is for hubId, the second dimension is for terminalId.
		/// Note that, in both client and server side, each terminal-hub is
		/// associated with an hub service. And a terminal-hub at client/server will communicate with
		/// companion terminal-hub at server/client. They detect opposite one via [hubId, terminalId].
		public readonly DkPhotonStreamHub[][] hubs;

		public PhotonStreamConnector(WebSocket socket, DkPhotonStreamClientInfo client) {
			this.socket = socket;
			this.hubs = new DkPhotonStreamHub[PhotonStreamServiceRegistry.STREAM_HUB_SERVICE_COUNT][];

			// Pass connector for the client.
			client.connector = this;

			// To communicate with terminal-hub from the client, for each hub, we create all pref-defined
			// terminal-hub list  which have same hubId, but different terminalId.
			// If some terminal-hub X come from the client, then X will
			// pair with some defined terminal-hub that be associated with the hub in server.
			var genHubTypes = PhotonStreamServiceRegistry.GeneratedStreamHubServiceTypes();
			for (int hubId = 0, hubCount = genHubTypes.Length; hubId < hubCount; ++hubId) {
				var terminalCount = PhotonStreamServiceRegistry.TerminalCount(hubId);
				var terminalHubs = hubs[hubId] = new DkPhotonStreamHub[terminalCount];

				for (int terminalId = 0; terminalId < terminalCount; ++terminalId) {
					// Each terminal-hub in server contains hubId and terminalId.
					var genHub = Activator.CreateInstance(genHubTypes[hubId], terminalId, client, this) as DkPhotonStreamHub;
					if (genHub == null) {
						throw new Exception($"Could not create hub: `{genHubTypes[hubId].Name}`");
					}
					terminalHubs[terminalId] = genHub;
				}
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
					// Just for reference.
					// case WebSocketMessageType.Text: {
					// 	var inMessage = Encoding.UTF8.GetString(buffer.Array!, 0, inResult.Count);
					// 	var messageToClient = Encoding.UTF8.GetBytes($"server[{this.GetHashCode()}]~ client-{userId} said: {inMessage}");
					// 	var messageToClientArr = new ArraySegment<byte>(messageToClient, 0, messageToClient.Length);
					// 	break;
					// }
					case WebSocketMessageType.Close: {
						// dkask: Send to client again??
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

		/// As requirement of MessagePack, we cannot make this method as async.
		private void ConsumeIncomingData(byte[] buffer, int offset, int count) {
			// dkopt: we should avoid copying buffer?
			// https://github.com/neuecc/MessagePack-CSharp#be-careful-when-copying-buffers
			//
			// Format of inData:
			// Service: [messageType(byte), hubId(byte), classId (byte), methodId(short), parameters(msgPackObj)]
			// RPC    : [messageType(byte), hubId(byte), classId (byte), methodId(short), rpcTarget(byte), parameters(msgPackObj)]
			var inData = new byte[count];
			Array.Copy(buffer, offset, inData, 0, count);

			var reader = new MessagePackReader(inData);
			var arrLength = reader.ReadArrayHeader(); // MUST read header first, otherwise get error.
			var messageType = (DkPhotonMessageType)reader.ReadByte(); // total max 256 types

			// For ping operation, just pong.
			if (messageType == DkPhotonMessageType.PING) {
				SendAsync(inData);
				return;
			}

			var hubId = reader.ReadByte(); // total max 256 hubs/connection
			var terminalId = reader.ReadByte(); // total max 256 terminals/hub
			var methodId = reader.ReadInt16(); // max 64k methods/hub
			var terminalHub = (DkPhotonStreamHub)this.hubs[hubId][terminalId]; // always exists

			switch (messageType) {
				case DkPhotonMessageType.SERVICE: {
					var paramsOffset = (int)reader.Consumed;
					terminalHub.HandleServiceRequest(methodId, inData, paramsOffset, count - paramsOffset);
					break;
				}
				case DkPhotonMessageType.RPC: {
					var rpcTarget = (DkPhotonRpcTarget)reader.ReadByte();
					terminalHub.HandleRpcRequest(rpcTarget, inData);
					break;
				}
				default: {
					throw new Exception($"Invalid message type: {messageType}");
				}
			}
		}

		/// Called when the client disconnected from us.
		public void OnDisconnect() {
			this.socket = null;
		}
	}
}
