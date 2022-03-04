namespace Tool.Compet.Photon {
	/// Each stream service can be implemented by multiple hubServices.
	public interface DkIPhotonStreamService<TServiceResponse> {
		public Task HandleRpcRequest(DkPhotonRpcTarget rpcTarget, byte[] data);
	}
}
