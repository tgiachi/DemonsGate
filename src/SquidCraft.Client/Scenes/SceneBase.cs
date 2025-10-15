using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Collections;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Scenes;

public abstract class SceneBase : IScene
{
    protected readonly ILogger Logger;

    protected SceneBase(string name)
    {
        Name = name;
        Logger = Log.ForContext(GetType());
        Components = [];
    }

    public string Name { get; }

    public SCDrawableCollection<ISCDrawableComponent> Components { get; }

    public bool IsLoaded { get; private set; }

    public bool HasFocus { get; set; } = true;

    public void Load()
    {
        if (IsLoaded)
        {
            Logger.Warning("Scene {SceneName} is already loaded", Name);
            return;
        }

        Logger.Information("Loading scene: {SceneName}", Name);
        OnLoad();
        InitializeComponents();
        IsLoaded = true;
        Logger.Information("Scene loaded: {SceneName}", Name);
    }

    public void Unload()
    {
        if (!IsLoaded)
        {
            Logger.Warning("Scene {SceneName} is not loaded", Name);
            return;
        }

        Logger.Information("Unloading scene: {SceneName}", Name);
        OnUnload();
        Components.Clear();
        IsLoaded = false;
        Logger.Information("Scene unloaded: {SceneName}", Name);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsLoaded)
        {
            return;
        }

        OnUpdate(gameTime);

        Components.CheckForZIndexChanges();
        foreach (var component in Components.GetEnabledComponents())
        {
            component.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!IsLoaded)
        {
            return;
        }


        OnDraw(gameTime, spriteBatch);

        foreach (var component in Components.GetVisibleComponents())
        {
            component.Draw(gameTime, spriteBatch);
        }

    }

    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsLoaded || !HasFocus)
        {
            return;
        }

        OnHandleKeyboard(keyboardState, gameTime);

        foreach (var component in Components.GetEnabledComponents())
        {
            component.HandleKeyboard(keyboardState, gameTime);
        }
    }

    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsLoaded || !HasFocus)
        {
            return;
        }

        OnHandleMouse(mouseState, gameTime);

        foreach (var component in Components.GetEnabledComponents())
        {
            component.HandleMouse(mouseState, gameTime);
        }
    }

    protected abstract void OnLoad();

    protected abstract void OnUnload();

    protected abstract void OnUpdate(GameTime gameTime);

    protected abstract void OnDraw(GameTime gameTime, SpriteBatch spriteBatch);

    protected virtual void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    protected virtual void OnHandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }

    private void InitializeComponents()
    {
        foreach (var component in Components)
        {
            if (component is ISCInitializable initializable)
            {
                initializable.Initialize();
            }
        }
    }
}
