namespace SnakeRescue.Utils
{
    public static class Constants
    {
        // ─── Scenes ───────────────────────────────────────────
        public const string SCENE_MAIN_MENU    = "MainMenu";
        public const string SCENE_GAME         = "GameScene";
        public const string SCENE_LEVEL_SELECT = "LevelSelect";
        public const string SCENE_LOADING      = "Loading";

        // ─── Tags ─────────────────────────────────────────────
        public const string TAG_PRINCESS  = "Princess";
        public const string TAG_SNAKE     = "Snake";
        public const string TAG_HAZARD    = "Hazard";
        public const string TAG_OBJECT    = "PhysicsObject";
        public const string TAG_GROUND    = "Ground";
        public const string TAG_WALL      = "Wall";

        // ─── Layers ───────────────────────────────────────────
        public const string LAYER_CHARACTERS = "Characters";
        public const string LAYER_OBJECTS    = "Objects";
        public const string LAYER_HAZARDS    = "Hazards";
        public const string LAYER_GROUND     = "Ground";

        // ─── Physics ──────────────────────────────────────────
        public const float PHYSICS_GRAVITY        = -9.81f;
        public const float BALL_MASS              = 1.5f;
        public const float ROCK_MASS              = 3.0f;
        public const float WEIGHT_MASS            = 5.0f;
        public const float OBJECT_BOUNCE          = 0.2f;
        public const float OBJECT_FRICTION        = 0.4f;

        // ─── Snake ────────────────────────────────────────────
        public const float SNAKE_IDLE_SPEED       = 0f;
        public const float SNAKE_PATROL_SPEED     = 1.0f;
        public const float SNAKE_CHASE_SPEED      = 2.5f;
        public const float SNAKE_DETECTION_RADIUS = 5.0f;
        public const float SNAKE_ATTACK_RADIUS    = 0.8f;
        public const float SNAKE_PATH_UPDATE_RATE = 0.3f;

        // ─── Princess ─────────────────────────────────────────
        public const float PRINCESS_DANGER_RADIUS = 3.0f;
        public const float PRINCESS_PANIC_RADIUS  = 1.5f;

        // ─── Star Rating ──────────────────────────────────────
        public const float STAR_3_TIME_MULTIPLIER = 0.6f;
        public const float STAR_2_TIME_MULTIPLIER = 0.85f;
        public const int   STAR_3_MAX_ACTIONS     = 1;
        public const int   STAR_2_MAX_ACTIONS     = 2;

        // ─── Save System ──────────────────────────────────────
        public const string SAVE_KEY_PLAYER_DATA  = "PlayerData";
        public const string SAVE_KEY_LEVEL_PREFIX = "Level_";
        public const string SAVE_KEY_SETTINGS     = "Settings";

        // ─── Audio ────────────────────────────────────────────
        public const float AUDIO_MASTER_DEFAULT   = 1.0f;
        public const float AUDIO_MUSIC_DEFAULT    = 0.7f;
        public const float AUDIO_SFX_DEFAULT      = 1.0f;
        public const float AUDIO_FADE_DURATION    = 0.5f;

        // ─── UI ───────────────────────────────────────────────
        public const float UI_ANIMATION_DURATION  = 0.3f;
        public const float UI_RESULT_DELAY        = 1.2f;
        public const int   MAX_STARS_PER_LEVEL    = 3;
        public const int   HINT_UNLOCK_FAILS      = 3;

        // ─── Level ────────────────────────────────────────────
        public const int   TOTAL_MVP_LEVELS       = 20;
        public const float LEVEL_RESET_DELAY      = 0.8f;
        public const float CHAIN_REACTION_TIMEOUT = 5.0f;
    }
}
