namespace Tool.Compet.Photon {
	using MessagePack;

	/// This hub is for frame-based streaming like game, video,...
	public abstract class DkPhotonStreamHub : PhotonHub {
		/// Handle incoming message by calling the target method.
		public abstract void HandleServiceRequest(int methodId, byte[] data, int offset, int count);

		/// Handle incoming message with RPC method.
		public abstract Task HandleRpcRequest(DkPhotonRpcTarget rpcTarget, byte[] data);

		public readonly int terminalId;
		public PhotonStreamConnector connector;

		public DkPhotonStreamHub(int id, int terminalId, PhotonStreamConnector connector) : base(id) {
			this.terminalId = terminalId;
			this.connector = connector;
		}

		internal async Task SendAsServiceAsync(int methodId, object msgPackObj) {
			// dkopt: consider reduce byte length of message.
			await this.connector.SendAsync(MessagePackSerializer.Serialize(new object[] {
				(byte)DkPhotonMessageType.SERVICE, // message type (service, rpc,...)
				(byte)this.id, // hub id
				(byte)this.terminalId, // terminal id
				(short)methodId, // method id
				msgPackObj // parameters
			}));
		}

		internal async Task SendAsRpcAsync(int methodId, object msgPackObj) {
			// dkopt: consider reduce byte length of message.
			await this.connector.SendAsync(MessagePackSerializer.Serialize(new object[] {
				(byte)DkPhotonMessageType.RPC, // message type (service, rpc,...)
				(byte)this.id, // hub id
				(byte)this.terminalId, // terminal id
				(short)methodId, // method id
				msgPackObj // parameters
			}));
		}
	}
}
