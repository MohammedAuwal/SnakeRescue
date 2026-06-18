namespace SnakeRescue.Core
{
    /// <summary>
    /// All possible states the game can be in at any time.
    /// Only ONE state is active at a time.
    /// </summary>
    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        LevelSelect,
        LevelIntro,
        Playing,
        Paused,
        ChainReacting,
        LevelComplete,
        LevelFailed,
        Settings
    }

    /// <summary>
    /// All possible states the Princess can be in.
    /// Drives animations and emotional feedback.
    /// </summary>
    public enum PrincessState
    {
        Idle,
        Alert,
        Fear,
        Panic,
        Relief,
        Celebrating,
        Dead
    }

    /// <summary>
    /// All possible states the Snake can be in.
    /// Drives movement and threat logic.
    /// </summary>
    public enum SnakeState
    {
        Idle,
        Patrolling,
        Detecting,
        Chasing,
        Attacking,
        Stunned,
        Dead
    }

    /// <summary>
    /// Level completion result.
    /// </summary>
    public enum LevelResult
    {
        None,
        Victory,
        Failed_PrincessCaught,
        Failed_PrincessHazard,
        Failed_Timeout
    }

    /// <summary>
    /// Interactive object types in the world.
    /// </summary>
    public enum ObjectType
    {
        Ball,
        Rock,
        Weight,
        Gate,
        Pin,
        Rope,
        Lever,
        Water,
        Fire,
        Spike,
        Trap
    }

    /// <summary>
    /// Hazard types that can harm the princess or affect the snake.
    /// </summary>
    public enum HazardType
    {
        Fire,
        Spike,
        Water,
        Lava,
        Trap
    }

    /// <summary>
    /// Star rating earned on level completion.
    /// </summary>
    public enum StarRating
    {
        Zero  = 0,
        One   = 1,
        Two   = 2,
        Three = 3
    }
}
