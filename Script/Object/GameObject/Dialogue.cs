using Godot;
using Godot.Collections;

public partial class Dialogue : Node2D
{   
    private static PackedScene _prefab = null;
    public static PackedScene Prefab
    {
        get
        {
            if (_prefab == null)
            {
                _prefab = AssetManager.Instance.GetPrefab("Asset.Object.dialogue", null);
            }
            return _prefab;
        }
    }
    public static Dialogue CreateDialogue(Array<string> contents, Vector2 position, Node parent = null,
        float contentWaitTime = 1f,
        float charTime = 0.07f,
        float jitterStrength = 0f,
        float jitterFrequency = 10f
    )
    {
        Dialogue dialogue = Prefab.Instantiate<Dialogue>();
        dialogue.contents = contents;
        dialogue.contentWaitTime = contentWaitTime;
        dialogue.charTime = charTime;
        dialogue.jitterStrength = jitterStrength;
        dialogue.jitterFrequency = jitterFrequency;
        dialogue.state = "start";

        dialogue.GlobalPosition = position;

        if(parent == null)
        {
            parent = GameManager.Instance.game.smallObjectNode;
        }

        parent.AddChild(dialogue);

        return dialogue;
    }

    [Export] RichTextLabel label;
    [Export] AudioStreamPlayer2D audioPlayer;

    [Export] Array<string> contents;

    [Export] float contentWaitTime = 1f;
    [Export] float charTime = 0.07f;

    /// <summary>
    /// 文字抖动强度
    /// </summary>
    [Export] public float jitterStrength = 0f;
    /// <summary>
    /// 文字抖动频率
    /// </summary>
    [Export] public float jitterFrequency = 10f;
    // ==========================================

    [Export] string contentNow;
    [Export] int contentIndex;
    [Export] int contentIndexMax;
    [Export] char charNow;
    [Export] int charIndex;
    [Export] int charIndexMax;
    [Export] string sentence;
    
    /// <summary>
    /// 对话状态， start开始初始化、show正在展示、wait正在等待、end结束放映
    /// </summary>
    [Export] string state = "start";

    // 持有抖动效果实例
    private TextJitterEffect _jitterEffect;

    /// <summary>
    /// 信号：对话开始
    /// </summary>
    [Signal]
    public delegate void OnInitEventHandler();
    /// <summary>
    /// 信号：对话句子切换
    /// </summary>
    /// <param name="contentNow"></param>
    /// <param name="contentNext"></param>
    [Signal]
    public delegate void OnContentChangeEventHandler(string last, string next);
    /// <summary>
    /// 信号：对话结束
    /// </summary>
    [Signal]
    public delegate void OnEndEventHandler();

    public override void _Ready()
    {
        SetupJitterEffect();
    }

    /// <summary>
    /// 根据 jitterStrength 动态添加/移除抖动效果
    /// </summary>
    private void SetupJitterEffect()
    {
        if (label == null) return;

        if (jitterStrength > 0f)
        {
            if (_jitterEffect == null)
            {
                _jitterEffect = new TextJitterEffect();
                label.CustomEffects.Add(_jitterEffect);
            }
            _jitterEffect.strength = jitterStrength;
            _jitterEffect.frequency = jitterFrequency;
        }
        else
        {
            // 强度为 0：移除效果
            if (_jitterEffect != null)
            {
                label.CustomEffects.Remove(_jitterEffect);
                _jitterEffect = null;
            }
        }
    }

    public void LoadWithText(string text)
    {
        //从长文本中根据一定格式加载对话
    }

    float timer = 0;

    public override void _Process(double delta)
    {
        timer += (float)delta;
        switch (state)
        {
            case "start":
                timer = 0;
                charIndex = -1;
                contentIndex = 0;
                contentIndexMax = contents.Count - 1;
                contentNow = contents[contentIndex];
                charIndexMax = contentNow.Length - 1;
                sentence = "";
                EmitSignal(nameof(OnInit));
                state = "show";
            break;

            case "show":
                if(timer >= charTime)
                {
                    timer = 0;
                    charIndex += 1;
                    if(charIndex > charIndexMax)
                    {
                        contentIndex += 1;
                        state = "wait";
                        break;
                    }
                    charNow = contentNow[charIndex];
                    sentence += charNow;
                    audioPlayer.Play();
                    //应用抖动效果文字
                    if (jitterStrength > 0f)
                        label.Text = $"[jitter]{sentence}[/jitter]";
                    else
                        label.Text = sentence;
                }
            break;

            case "wait":
                if(timer >= contentWaitTime)
                {
                    timer = 0;
                    sentence = "";
                    if(contentIndex > contentIndexMax)
                    {
                        state = "end";
                        break;
                    }
                    else
                    {
                        charIndex = -1;
                        charIndexMax = contents[contentIndex].Length - 1;
                        string lastContent = contentNow;
                        contentNow = contents[contentIndex];
                        EmitSignal(nameof(OnContentChange), lastContent, contentNow);
                        state = "show";
                        break;
                    }
                }
            break;

            case "end":

                EmitSignal(nameof(OnEnd));
                QueueFree();

            break;
        }
    }


} 
