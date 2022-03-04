namespace Tool.Compet.Photon {
	///
	/// This is intermediate class which handles communication between PhotonConnector and AppHubService.
	///
	/// In detail, it does:
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
	public class DkPhotonStreamServer {
		// public static TServiceResponse CreateServiceResponse<TServiceResponse>(int hubId, object client, PhotonStreamConnector connector) {
		// 	return null;
		// }
	}
}
