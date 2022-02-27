namespace Tool.Compet.Photon {
	public interface IPhotonService {
		/// Handle incoming message by calling the target method.
		public void HandleServiceRequest(int methodId, byte[] data, int offset, int count);

		/// Handle incoming message with RPC method.
		public Task HandleRpcRequest(DkPhotonRpcTarget rpcTarget, byte[] data);
	}
}
