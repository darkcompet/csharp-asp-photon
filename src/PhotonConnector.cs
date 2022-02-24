namespace Tool.Compet.Photon {
	using System;
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Text;
	using MessagePack;
	using Tool.Compet.Log;

	/// This uses auto-generated class `PhotonHubRegistry` to register hubs.
	/// The app must run Photon generator first.
	public class PhotonConnector {
		/// Be released when the client get disconnected.
		private object client;

		/// Raw communicator (send/receive) between server and the client.
		/// Be released when the client get disconnected.
		private WebSocket socket;

		/// Holds all hubs for communication between server and the client.
		public readonly Dictionary<int, PhotonHub> hubs = new();

		public PhotonConnector(object client, WebSocket socket) {
			this.client = client;
			this.socket = socket;

			// [Configure hub, connector and client]
			var hubList = new List<PhotonHub>();

			foreach (var (hubId, hubType) in PhotonServiceRegistry.hubTypes) {
				// Create new hubs for the client.
				var hub = Activator.CreateInstance(hubType, hubId, client, this) as PhotonHub;
				if (hub == null) {
					throw new Exception($"Could not create hub instance of: {hubType.Name}");
				}
				hubList.Add(hub);
			}

			// Register hubs
			foreach (var hub in hubList) {
				this.hubs[hub.id] = hub;
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

		/// Get data from client.
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
		/// and tell target hub sending message to clients.
		public async Task HandleCommunication() {
			// We provide 4 KB buffer to hold incoming message from the client.
			var readBuffer = new ArraySegment<byte>(new byte[1 << 12]);
			var inData = await socket.ReceiveAsync(readBuffer, CancellationToken.None);

			// [Step 2. Realtime process]
			// This is called after handshaking completed.
			// Connection at this time is websocket, NOT http.
			// We just loop until the connection get disconnected from client or program exited.
			// while (webSocket.State == WebSocketState.Open) { }
			while (!inData.CloseStatus.HasValue) {
				// Console.WriteLine($"Total {InMemory.users.Count} users, webSocket.State: {socket.State}");

				// Need check message type?
				switch (inData.MessageType) {
					case WebSocketMessageType.Binary: {
						// Console.WriteLine($"incomingResult.MessageType: Binary, length: {inData.Count}");

						/// Need branch to new method as MessagePack's requirement.
						ConsumeIncomingData(readBuffer.Array!, 0, inData.Count);

						// foreach (var user in InMemory.users) {
						// 	await user.Value.SendAsync(
						// 		new ArraySegment<byte>(buffer.Array!, 0, inResult.Count),
						// 		WebSocketMessageType.Binary,
						// 		true,
						// 		CancellationToken.None
						// 	);
						// 	if (BuildConfig.DEBUG) { AppLogs.Debug(this, $"Sent binary to user: {user.Key}"); }
						// }
						break;
					}
					case WebSocketMessageType.Close: {
						Console.WriteLine("incomingResult.MessageType: Close");
						await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
						break;
					}
					// case WebSocketMessageType.Text: {
					// 	Console.WriteLine("incomingResult.MessageType: Text");
					// 	// Send something to client
					// 	var inMessage = Encoding.UTF8.GetString(buffer.Array!, 0, inResult.Count);

					// 	if ("bye".Equals(inMessage)) {
					// 		var byeMessage = "server~ bye client";
					// 		await socket.SendAsync(
					// 			new ArraySegment<byte>(Encoding.UTF8.GetBytes(byeMessage), 0, byeMessage.Length),
					// 			WebSocketMessageType.Text,
					// 			true,
					// 			CancellationToken.None
					// 		);
					// 		await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client has closed the connection", CancellationToken.None);
					// 		break;
					// 	}

					// 	var messageToClient = Encoding.UTF8.GetBytes($"server[{this.GetHashCode()}]~ client-{userId} said: {inMessage}");
					// 	var messageToClientArr = new ArraySegment<byte>(messageToClient, 0, messageToClient.Length);

					// 	DkLogs.Debug(this, $"inMessage: {inMessage}, incomingResult.MessageType: {inResult.MessageType}, incomingResult.EndOfMessage: {inResult.EndOfMessage}");

					// 	foreach (var user in InMemory.users) {
					// 		await user.Value.SendAsync(
					// 			messageToClientArr,
					// 			WebSocketMessageType.Text,
					// 			true,
					// 			CancellationToken.None
					// 		);
					// 		// if (BuildConfig.DEBUG) { AppLogs.Debug(this, $"Sent text to user: {user.Key}"); }
					// 	}
					// 	break;
					// }
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
			var inData = new byte[count];
			Array.Copy(buffer, offset, inData, 0, count);

			var reader = new MessagePackReader(inData);
			var arrLength = reader.ReadArrayHeader();

			var messageType = (DkPhotonMessageType)reader.ReadByte(); // total max 256 types
			var hubId = reader.ReadByte(); // total max 256 hubs/connection
			var methodId = reader.ReadInt16(); // max 64k methods/hub

			var hub = this.hubs[hubId];

			switch (messageType) {
				case DkPhotonMessageType.SERVICE: {
					var paramsOffset = (int)reader.Consumed;
					hub.CallServiceMethod(methodId, inData, paramsOffset, count - paramsOffset);
					break;
				}
				case DkPhotonMessageType.RPC: {
					var rpcTarget = (DkPhotonRpcTarget)reader.ReadByte();
					hub.HandleRPC(rpcTarget, inData);
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
