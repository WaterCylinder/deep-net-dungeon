using Godot;
using System;

public static class Global
{
    public const string GAME_VERSION = "0.1.0";
    public const int DEFAULT_FPS = 60;
    public const float DEFAULT_TIMESCALE = 1f;

    // 玩家配置
    public const float PLAYER_DEFAULT_SPEED = 200f;
    public const float PLAYER_DEFAULT_HEALTH = 100f;
    public const float PLAYER_DEFAULT_DAMAGE = 10f;

    // 敌人配置
    public const float ENEMY_DEFAULT_SPEED = 150f;
    public const float ENEMY_DEFAULT_HEALTH = 50f;
    public const float ENEMY_DEFAULT_DAMAGE = 10f;

    // 弹幕配置
    public const float BULLET_DEFAULT_SPEED = 300f;
    public const float BULLET_DEFAULT_DAMAGE = 10f;
    public const float BULLET_LIFETIME = 10f;

    // 关卡配置
    public const int TOTAL_LEVELS = 5;
    public const int MIN_WAVES = 3;
    public const int MAX_WAVES = 10;

    // 道具配置
    public const int PASSIVE_ITEM_LIMIT = 99;
    public const int ACTIVE_ITEM_LIMIT = 1;

    // 音频配置
    public const float MASTER_VOLUME = 0.8f;
    public const float MUSIC_VOLUME = 0.8f;
    public const float SFX_VOLUME = 0.8f;

    // 路径配置
    public const string SAVE_PATH = "user://saves/";
    public const string RESOURCE_PATH = "res://Resource/";
    public const string ASSET_PATH = "res://Asset/";
    public const string SCENE_PATH = "res://Scene/";

    // UI配置
    public const float UI_FADE_SPEED = 0.5f;
    public const float UI_SCALE_SPEED = 0.3f;
}
