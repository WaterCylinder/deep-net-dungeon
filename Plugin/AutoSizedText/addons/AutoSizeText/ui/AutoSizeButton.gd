@tool
class_name AutoSizeButton extends Button

@export_multiline
var button_text: String = "":
	get:
		return button_text
	set(value):
		button_text = value
		
		notify_property_list_changed()
		_sync_label()
		_update_label()

@export_tool_button("FORCE REFRESH")
var refresh_button: Callable = RequestResizeText

@export_group("Auto Font Size")

## Min text size to reach
@export_range(1, 512, 1.0)
var min_font_size: int = 8:
	get:
		return min_font_size
	set(value):
		min_font_size = value
		if min_font_size >= max_font_size:
			min_font_size = max_font_size - 1
			push_warning(
				"min_font_size {0} >= max_font_size {1}, fixed to {2}"
				.format([value, max_font_size, min_font_size])
			)

		notify_property_list_changed()
		_sync_label()
		_update_label()

## Max text size to reach
@export_range(1, 512, 1.0)
var max_font_size: int = 38:
	get:
		return max_font_size
	set(value):
		max_font_size = value
		if max_font_size <= min_font_size:
			max_font_size = min_font_size + 1
			push_warning(
				"max_font_size {0} <= min_font_size {1}, fixed to {2}"
				.format([value, min_font_size, max_font_size])
			)

		notify_property_list_changed()
		_sync_label()
		_update_label()

@export_group("Step Size")

## Needs 2 numbers to work / will be automatically prefered over "Auto-Size"[br]
## when 2 numbers or more are present.
@export
var step_sizes: Array[int] = []:
	get:
		return step_sizes
	set(value):
		step_sizes = value
		step_sizes.sort()

		notify_property_list_changed()
		_sync_label()
		_update_label()


var _label: AutoSizeLabel
var _saved_theme_colors: Dictionary[String, Color]

func _ready() -> void:
	set(&"theme_override_font_sizes/font_size", 1)
	_prepare_colors()

	clip_text = true
	
	if _label == null:
		_label = AutoSizeLabel.new()
		_label.auto_register_refresh = false
		_label.force_default_settings()
		add_child(_label, false, Node.INTERNAL_MODE_BACK)
		_label.size = size
		_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		_label.set_anchors_preset(PRESET_FULL_RECT)
		
	AutoSizeTextRefresh.Register(self)
	_sync_label()
	RequestResizeText()

func RequestResizeText() -> void:
	call_deferred(&"_update_label")

func _sync_label() -> void:
	if _label == null:
		return
		
	_label.min_font_size = min_font_size
	_label.max_font_size = max_font_size
	_label.step_sizes = step_sizes

func _update_label() -> void:
	if _label == null:
		return
		
	_label.text = button_text
	_label.auto_translate_mode = auto_translate_mode
	_sync_color("font_color")
	_label.call_deferred(&"do_resize_text")
	
	if Engine.is_editor_hint() and text != "":
		push_warning(
			"The AutoSizeButton '%s' has the text '%s'. Please set the text to 'button_text' instead of the text property."
			 % [name, text]
		)
	
func _prepare_colors() -> void:
	const colors_to_disable: Array[String] = [
		"font_color",
		"font_disabled_color",
		"font_hover_pressed_color",
		"font_hover_color",
		"font_focus_color",
		"font_pressed_color",
	]
	
	for color_name: String in colors_to_disable:
		_saved_theme_colors[color_name] = get_theme_color(color_name, "Button")
		
		if not Engine.is_editor_hint():
			set("theme_override_colors/" + color_name, Color(0, 0, 0, 0))

func _sync_color(color_type: String) -> void:
	if _label == null:
		return

	_label.set(&"theme_override_colors/font_color", _saved_theme_colors[color_type])
