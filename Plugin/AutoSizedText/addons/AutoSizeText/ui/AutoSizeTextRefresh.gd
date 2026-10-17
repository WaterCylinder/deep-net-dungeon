@tool
class_name AutoSizeTextRefresh
extends RefCounted

const AUTO_SIZE_TEXT_GROUP := &"auto_size_text_controls"
static var translationWatcher: TranslationWatcher = null
static var isAddingWatcher: bool = false
static var refreshQueued: bool = false

class TranslationWatcher extends Node:
	func _enter_tree() -> void:
		AutoSizeTextRefresh.isAddingWatcher = false

	func _exit_tree() -> void:
		if AutoSizeTextRefresh.translationWatcher == self:
			AutoSizeTextRefresh.translationWatcher = null

	func _notification(what: int) -> void:
		if what != NOTIFICATION_TRANSLATION_CHANGED:
			return

		AutoSizeTextRefresh.RequestRefreshAll(get_tree())

static func Register(node: Node) -> void:
	node.add_to_group(AUTO_SIZE_TEXT_GROUP)
	EnsureTranslationWatcher(node)

static func EnsureTranslationWatcher(sourceNode: Node) -> void:
	if isAddingWatcher:
		return

	if translationWatcher != null and is_instance_valid(translationWatcher):
		return

	if not sourceNode.is_inside_tree():
		return

	isAddingWatcher = true
	translationWatcher = TranslationWatcher.new()
	translationWatcher.name = "AutoSizeTextTranslationWatcher"

	sourceNode.get_tree().root.call_deferred(
		&"add_child",
		translationWatcher,
		false,
		Node.INTERNAL_MODE_BACK
	)

static func RequestRefreshAll(tree: SceneTree) -> void:
	if refreshQueued:
		return
	refreshQueued = true
	await tree.process_frame
	await tree.process_frame
	await tree.process_frame
	ResizeAll(tree)
	refreshQueued = false

static func ResizeAll(tree: SceneTree) -> void:
	tree.call_group_flags(
		SceneTree.GROUP_CALL_DEFERRED | SceneTree.GROUP_CALL_UNIQUE,
		AUTO_SIZE_TEXT_GROUP,
		&"RequestResizeText"
	)
