namespace Tool.Compet.Photon {
	using System;
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Text;
	using Tool.Compet.Log;

	public class DkPhotonConnector {
		internal Dictionary<int, PhotonHub> hubs;

		public WebSocket socket;

		public DkPhotonConnector(WebSocket socket) {
			this.socket = socket;
		}

		public async Task HandleCommunication() {
			// [Step 1. Connection established (after handshaking)]
			// We provide 4 KB buffer for each client.
			var buffer = new ArraySegment<byte>(new byte[1 << 12]);
			// Incoming message from the client.
			var inResult = await socket.ReceiveAsync(buffer, CancellationToken.None);

			// [Step 2. Realtime process]
			// This is called after handshaking completed.
			// Connection at this time is websocket, NOT http.
			// We just loop until the connection get disconnected from client or program exited.
			// while (webSocket.State == WebSocketState.Open) {
			// }
			while (!inResult.CloseStatus.HasValue) {
				// Console.WriteLine($"Total {InMemory.users.Count} users, webSocket.State: {socket.State}");

				// Need check message type?
				switch (inResult.MessageType) {
					case WebSocketMessageType.Binary: {
						Console.WriteLine($"incomingResult.MessageType: Binary, length: {inResult.Count}");

						ConsumeData(buffer.Array!, 0, inResult.Count);

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
						Console.WriteLine($"Unhandled incomingResult.MessageType: {inResult.MessageType}");
						break;
					}
				}

				// Read next incoming data
				inResult = await socket.ReceiveAsync(buffer, CancellationToken.None);
			}
		}

		private void ConsumeData(byte[] data, int offset, int count) {
			// call hubs
		}
	}
}
