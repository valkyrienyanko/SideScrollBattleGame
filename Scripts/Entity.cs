﻿namespace SideScrollGame;

public partial class Entity : Node2D, IDamageable
{
    [Export] public Team   Team                   { get; set; } = Team.Left;
    [Export] public double MaxHealth              { get; set; } = 100;
    [Export] public float  MoveSpeed              { get; set; } = 1;
    [Export] public int    AttackPower            { get; set; } = 10;
    [Export] public float  DetectionRange         { get; set; } = 10;
    [Export] public int    AttackCooldownDuration { get; set; } = 1000; // in ms

    public bool Destroyed => GodotObject.IsInstanceValid(this);
    public Dictionary<StateType, State<Entity>> States { get; set; } = new();
    public AnimatedSprite2D   AnimatedSprite      { get; set; }
    public Area2D             DetectionArea       { get; set; }
    public TextureProgressBar HealthBar           { get; set; }
    public Vector2            SpriteSize          { get; set; }
    public StateType          CurrentState        { get; set; }
    public Team               OtherTeam           { get; set; }
    public bool               FoundEnemy          { get; set; }
    public List<IDamageable>  DetectedEnemies     { get; set; } = new();
    public Tween              AttackTween         { get; set; }

    public double CurHealth 
    { 
        get => HealthBar.Value;
        set 
        {
            if (value <= 0)
            {
                QueueFree();
                return;
            }

            HealthBar.Value = value;
        }
    }

    public override void _Ready()
    {
        AddToGroup(Team.ToString());
        AnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        AnimationPlayer = AnimatedSprite.GetNode<AnimationPlayer>("AnimationPlayer");
        TimerAttackCooldown = new GTimer(
            this, 
            () => GD.Print("todo: switch to find state"), 
            AttackCooldownDuration);

        AnimationPlayer.AnimationFinished += (anim) =>
        {
            if (anim == "attack")
            {
                GD.Print("todo: switch to cooldown state");
                TimerAttackCooldown.StartMs();
            }
        };

        if (Team == Team.Left)
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
            AnimatedSprite.InstantPlay("move");

        // Create the Area2D for this sprite. All other areas will try to detect this area
        SpriteSize = AnimatedSprite.GetSize("move");
        CreateBodyArea();
        CreateDetectionArea();
        CreateHealthBar();

        States[StateType.Attack]   = new StateAttack(this);
        States[StateType.Cooldown] = new StateCooldown(this);
        States[StateType.Find]     = new StateFind(this);
        States[StateType.Move]     = new StateMove(this);

        CurrentState = StateType.Move;

        States[CurrentState].EnterState();
    }

    public override void _PhysicsProcess(double delta)
    {
        States[CurrentState].Update();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("view_health"))
            HealthBar.Show();

        if (Input.IsActionJustReleased("view_health"))
            HealthBar.Hide();
    }

    public void ValidateDetectedEnemies()
    {
        for (int i = 0; i < DetectedEnemies.Count; i++)
        {
            if (!DetectedEnemies[i].Destroyed)
            {
                DetectedEnemies.RemoveAt(i);
                continue;
            }
        }
    }

    // This function is called from within the AnimationPlayer track
    private void Attack()
    {
        ValidateDetectedEnemies();
        foreach (var entity in DetectedEnemies)
        {
            entity.CurHealth -= AttackPower;
            break;
        }
    }

    private void CreateBodyArea()
    {
        var area = new Area2D();
        var collisionShape = new CollisionShape2D
        {
            Shape = new RectangleShape2D
            {
                Size = SpriteSize
            }
        };

        area.AddChild(collisionShape);
        AddChild(area);
    }

    private void CreateDetectionArea()
    {
        var detectionHeight = 100;

        var detectionPos = SpriteSize.X / 2 + DetectionRange / 2;

        DetectionArea = new Area2D();
        var collisionShape = new CollisionShape2D
        {
            Position = new Vector2(detectionPos, 0),
            Shape = new RectangleShape2D
            {
                Size = new Vector2(DetectionRange, detectionHeight)
            }
        };

        DetectionArea.AreaEntered += (otherArea) =>
        {
            var parent = otherArea.GetParent();

            if (parent.IsInGroup(OtherTeam.ToString()) 
                && parent is IDamageable damageable)
            {
                DetectedEnemies.Add(damageable);
                AnimatedSprite.InstantPlay("idle");
                FoundEnemy = true;
            }
        };

        DetectionArea.AreaExited += (otherArea) =>
        {
            if (otherArea.IsInGroup(OtherTeam.ToString()))
            {
                DetectedEnemies.Remove(otherArea.GetParent<Entity>());
            }
        };

        DetectionArea.AddChild(collisionShape);
        AddChild(DetectionArea);
    }

    private void CreateHealthBar()
    {
        HealthBar = Prefabs.HealthBar.Instantiate<TextureProgressBar>();
        HealthBar.MaxValue = MaxHealth;
        HealthBar.Value = MaxHealth;
        HealthBar.Position = new Vector2(-HealthBar.Size.X / 2, -SpriteSize.Y / 2 - 3);
        HealthBar.Hide();
        AddChild(HealthBar);
    }
}

public enum Team
{
    Left,
    Right
}
