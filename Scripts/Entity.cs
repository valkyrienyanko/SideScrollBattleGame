﻿namespace SideScrollGame;

public abstract partial class Entity : Node2D
{
    public abstract Team MyTeam { get; }

    public virtual void Init() { }
    public virtual void Update() { }

    public override void _Ready()
    {
        AnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        AnimationPlayer = AnimatedSprite.GetNode<AnimationPlayer>("AnimationPlayer");
        TimerAttackCooldown = new GTimer(
            this, 
            () => State = State.Find, 
            AttackCooldownDuration);

        AnimationPlayer.AnimationFinished += (anim) =>
        {
            if (anim == "attack")
            {
                State = State.Cooldown;
                TimerAttackCooldown.Start();
            }
        };

        if (MyTeam == Team.Left)
        {
            OtherTeam = Team.Right;

            // All sprites face the right side by default
        }
        else
        {
            OtherTeam = Team.Left;

            // Flip the root node to face the left side
            Scale = new Vector2(Scale.X * -1, Scale.Y);
        }

        // Play the 'move' animation set at a random starting frame
        if (AnimatedSprite.SpriteFrames.HasAnimation("move"))
            AnimatedSprite.PlayRandom("move");

        // Create the Area2D for this sprite. All other areas will try to detect this area
        var spriteSize = AnimatedSprite.GetSize("move");
        CreateBodyArea(spriteSize);
        CreateDetectionArea(spriteSize);

        State = State.Moving;

        Init();
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (State)
        {
            case State.Moving:
                if (!FoundEnemy)
                    Position += MyTeam == Team.Left ?
                        new Vector2(1, 0) : new Vector2(-1, 0);
                else
                {
                    State = State.Attack;
                }
                break;
            case State.Attack:
                AnimationPlayer.Play("attack");
                break;
            case State.Find:
                FoundEnemy = false;
                foreach (var area in DetectionArea.GetOverlappingAreas())
                {
                    if (!area.IsInGroup(MyTeam.ToString()))
                    {
                        FoundEnemy = true;
                        State = State.Attack;
                        break;
                    }
                }
                break;
            default:
                break;
        }

        Update();
    }

    private void OnHit()
    {
        GD.Print("Hit!");
    }

    private AnimatedSprite2D AnimatedSprite { get; set; }
    private AnimationPlayer AnimationPlayer { get; set; }
    private GTimer TimerAttackCooldown { get; set; }
    private Area2D DetectionArea { get; set; }
    private State State { get; set; }
    private Team OtherTeam { get; set; }
    private bool FoundEnemy { get; set; }
    private int AttackCooldownDuration { get; } = 1000; // in ms

    private void CreateBodyArea(Vector2 spriteSize)
    {
        var area = new Area2D();
        area.AddToGroup(MyTeam.ToString());
        var collisionShape = new CollisionShape2D
        {
            Shape = new RectangleShape2D
            {
                Size = spriteSize
            }
        };

        area.AddChild(collisionShape);
        AddChild(area);
    }

    private void CreateDetectionArea(Vector2 spriteSize)
    {
        var detectionWidth = 10;
        var detectionHeight = 100;

        var detectionPos = spriteSize.X / 2 + detectionWidth / 2;

        DetectionArea = new Area2D();
        var collisionShape = new CollisionShape2D
        {
            Position = new Vector2(detectionPos, 0),
            Shape = new RectangleShape2D
            {
                Size = new Vector2(detectionWidth, detectionHeight)
            }
        };

        DetectionArea.AreaEntered += (otherArea) =>
        {
            if (otherArea.IsInGroup(OtherTeam.ToString()))
            {
                AnimatedSprite.Play("idle");
                FoundEnemy = true;
            }
        };

        DetectionArea.AddChild(collisionShape);
        AddChild(DetectionArea);
    }
}

public enum State
{
    Moving,
    Attack,
    Cooldown,
    Find
}

public enum Team
{
    Left,
    Right
}
