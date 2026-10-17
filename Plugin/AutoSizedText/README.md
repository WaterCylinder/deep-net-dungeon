## AutoSizeText
It autosizes text in labels and other nodes that has text in it.
Perfect for projects that has UI in them. A must have if you want to implement localization.

Just use the auto size equivalent instead of the built-in ones, or change your old ones.
You are good to go.

## What is different
1) The original was checking if the text was changed every, single, frame for each node...
With this, it resizes once. Whenever the localization is changed, all the text get resized as well.
You can resize all the text or some particular text yourself as well, if that is what you want.

2) Now it works in the editor properly. You can easily preview text in different languages easily as well.

## Features
* Font Auto Size: Change Font-Size between two numbers
* Font Step Size: Change Font-Size based on pre-defined numbers
* Step-Size based on theme
* Auto-Size numbers based on theme

## Current Classes
* Label
* Button
* CheckButton
* CheckBox
* RichTextLabel
* TextEdit
* LineEdit
* MenuButton
* OptionButton

All thanks goes to bison - SpielmannSpiel. I just made it fit into my own needs and optimized it.

#Original
Godot Asset Library: https://godotengine.org/asset-library/asset/3843  
GitHub: https://github.com/SpielmannSpiel/AutoSizeText
by bison - SpielmannSpiel https://spielmannspiel.com
