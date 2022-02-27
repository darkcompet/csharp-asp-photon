namespace Tool.Compet.Photon {
	using System.Threading.Tasks;

	/// This is intermediate class which handles communication between PhotonConnector and AppService.
	///
	/// In detail, it will:
	/// + [Handle request]:
	///   - Parse incoming message from PhotonConnector.
	///   - Detect/Call target methods in AppService with provided parameters from the message.
	/// + [Handle response]:
	///   - Provide method-invocation response for AppService.
	///   - Serialize method-parameters, pass it to PhotonConnector.
	///
	/// Image as below:
	///                          <-- request <--
	///                         /               \
	/// AppService <-- ServiceWrapper --> PhotonConnector
	///      \                /
	///       --> response -->
	///
	/// How to use:
	/// Subclass should extends this hub service, then implement services like `HandleRpcRequest()`.
	public abstract class DkPhotonStreamHubService<TServiceRepsonse> : DkPhotonStreamHub, DkIPhotonService<TServiceRepsonse> {
		/// Handle request-method from the client.
		private DkIPhotonService<TServiceRepsonse> service;

		/// Actually, it calls response-method in the client's hub.
		protected TServiceRepsonse response;

		public DkPhotonStreamHubService(int id, PhotonConnector connector) : base(id, connector) {
			var serviceWrapper = PhotonServiceRegistry.CreateServiceWrapper(this);
			this.service = (DkIPhotonService<TServiceRepsonse>)serviceWrapper;
			this.response = (TServiceRepsonse)serviceWrapper;
		}

		/// Handle incoming message by calling the target method.
		public void HandleServiceRequest(int methodId, byte[] data, int offset, int count) {
			this.service.HandleServiceRequest(methodId, data, offset, count);
		}

		/// Handle incoming message with RPC method.
		public abstract Task HandleRpcRequest(DkPhotonRpcTarget rpcTarget, byte[] data);
	}
}
