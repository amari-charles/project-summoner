class_name AsyncUtils
extends RefCounted

## Await a signal with timeout protection to prevent hangs.
## Returns true if signal was received, false if timed out.
## Must be called from a Node context (pass the scene tree).
static func await_signal_with_timeout(tree: SceneTree, sig: Signal, timeout_seconds: float) -> bool:
	var state: Array[bool] = [false]
	sig.connect(func() -> void: state[0] = true, CONNECT_ONE_SHOT)

	var elapsed: float = 0.0
	while not state[0] and elapsed < timeout_seconds:
		await tree.process_frame
		elapsed += tree.root.get_process_delta_time()

	return state[0]
