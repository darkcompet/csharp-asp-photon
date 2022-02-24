namespace Tool.Compet.Photon {
	using MessagePack;

	public abstract class PhotonHub {
		/// Handle incoming message by calling the target method.
		public abstract void CallServiceMethod(int methodId, byte[] data, int offset, int count);

		/// Handle incoming message with RPC method.
		public abstract Task HandleRPC(DkPhotonRpcTarget rpcTarget, byte[] data);

		public readonly int id;
		public PhotonConnector connector;

		public PhotonHub(int id, PhotonConnector connector) {
			this.id = id;
			this.connector = connector;
		}

		internal async Task SendAsync(int methodId, object msgPackObj) {
			// dkopt: consider convert to lesser bytes to send.
			await this.connector.SendAsync(MessagePackSerializer.Serialize(new object[] {
				(byte)DkPhotonMessageType.SERVICE, // message type (service, rpc,...)
				(byte)this.id, // hub id
				(short)methodId, // method id
				msgPackObj // parameters
			}));
		}
	}
}
