using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using demo.uicontrols;
using System.Reflection;
using System.Runtime.InteropServices;
using Beetle;
using System.Threading;
using System.Xml;
using System.Diagnostics;
using ProjectMercury;
using ProjectMercury.Renderers;
using demo.animation;
using Microsoft.Xna.Framework.Input.Touch;
using System.Xml.Linq;




namespace demo
{


    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public partial class MainGame : Microsoft.Xna.Framework.Game
    {
        public enum EditorOp
        {
            Move,
            Copy,
            Cut,
            Paste,
            Delete,
            Create,
            Save,
            Scale,
        }

        
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private Scene CurrentScene;
        private Player player;
        private SpriteFont mainfont;
        //private static TcpChannel clientchannel;
        private static Beetle.ProtoBufAdapter.ProtoBufChannel clientchannel = new Beetle.ProtoBufAdapter.ProtoBufChannel();
        private long ClientID;
        private SpriteBatchRenderer spritebatchrenderer;
        private bool ContentLoadCompleted = false;

        private ParticleEffect peTrails;
        private ParticleEffect peSpawn;
        private ParticleEffect peClick;

        private Texture2D loadingTexture;

        public static bool IsEditorMode { get; set; }
        Texture2D[] cloudTextureArray = new Texture2D[15];

        public MainGame()
        {
            graphics = new GraphicsDeviceManager(this);
            GameConst.ScreenWidth = 800;
            GameConst.ScreenHeight = 480;
            GameConst.Graphics = graphics;
            GameConst.Content = Content;
            GameConst.GameWindow = this.Window;
            graphics.PreferredBackBufferWidth = GameConst.ScreenWidth;
            graphics.PreferredBackBufferHeight = GameConst.ScreenHeight;
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft;
            graphics.IsFullScreen = true;
            graphics.DeviceCreated += new EventHandler<EventArgs>(Graphics_DeviceCreated);
            Content.RootDirectory = "Content";
        }
#if WINDOWS
        /// <summary>
        /// 窗口内按键弹起消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_KeyUp(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == System.Windows.Forms.Keys.E && args.Control)
            {
                MainGame.IsEditorMode = !MainGame.IsEditorMode;
                Control window = System.Windows.Forms.Control.FromHandle(GameConst.GameWindow.Handle);
                if (MainGame.IsEditorMode)
                {
                    window.Invoke(new Action(() =>
                    {
                        this.Window.Title = "Editor mode";
                    }));
                }
                else
                {
                    if (player != null)
                    {
                        window.Invoke(new Action(() =>
                        {
                            this.Window.Title = player.Name;
                        }));
                        player.UpdateSceneScroll();
                    }
                }
            }
            else if (args.KeyCode == System.Windows.Forms.Keys.S && args.Control)
            {
                if (MainGame.IsEditorMode)
                {
                    CurrentScene.EditorOperate(EditorOp.Save, 0, 0);
                }
            }
            else if (args.KeyCode == System.Windows.Forms.Keys.S)
            {
                if (!MainGame.IsEditorMode)
                    CurrentScene.IntoBattle();
            }
        }
        /// <summary>
        /// 窗口内按键按下消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_KeyDown(object sender, KeyEventArgs args)
        {

        }
        /// <summary>
        /// 窗口移动消息消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_Move(object sender, EventArgs args)
        {
            if (dlgLogin != null)
            {
                dlgLogin.Location = new System.Drawing.Point((this.Window.ClientBounds.Width - dlgLogin.Size.Width) / 2 + this.Window.ClientBounds.X,
                                                        (this.Window.ClientBounds.Height - dlgLogin.Size.Height) * 3 / 4 + this.Window.ClientBounds.Y);
            }
        }
        /// <summary>
        /// 鼠标在窗口内按下消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_MouseDown(object sender, MouseEventArgs args)
        {
            UIMgr.HandleMessage(UIMessage.MouseDown, args.X, args.Y);
        }
        /// <summary>
        /// 鼠标在窗口内点击消息Callback
        /// </summary>
        Vector3 clickPos = new Vector3();
        protected void MainWindow_MouseClick(object sender, MouseEventArgs args)
        {
            int result = UIMgr.HandleMessage(UIMessage.MouseClick, args.X, args.Y);
            if (result != 0)
                return;

            if (!MainGame.IsEditorMode)
            {
                if (args.Button == MouseButtons.Left)
                {
                    OnMouseLeftClick(args.X, args.Y);
                }
            }
        }
        private int _mousex;
        private int _mousey;
        private MouseButtons _mousebutton;
        /// <summary>
        /// 鼠标在窗口内移动消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_MouseMove(object sender, MouseEventArgs args)
        {
            _mousex = args.X;
            _mousey = args.Y;
            _mousebutton = args.Button;
            int result = UIMgr.HandleMessage(UIMessage.MouseMove, args.X, args.Y);
            if (result != 0)
                return;
            if (MainGame.IsEditorMode)
            {
                if (args.Button == MouseButtons.Left)
                {
                    CurrentScene.EditorOperate(EditorOp.Move, args.X, args.Y);
                    CurrentScene.HighLightChunkByPoint(args.X, args.Y);

                    Vector3 p = new Vector3(args.X, args.Y, 0);
                    peTrails.Trigger(ref p);
                }
                else
                {
                    CurrentScene.HighLightChunkByPoint(args.X, args.Y);
                }
            }
            else
            {
                CurrentScene.HighLightCharacterByPoint(args.X, args.Y);
            }
        }
        /// <summary>
        /// 鼠标按键在窗口内弹起消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_MouseUp(object sender, MouseEventArgs args)
        {
            if (MainGame.IsEditorMode)
            {
                if (args.Button == MouseButtons.Left)
                {
                    CurrentScene.DoEditorMenu(0, 0, false);
                }
                else if (args.Button == MouseButtons.Right)
                {
                    CurrentScene.DoEditorMenu(args.X, args.Y, true);
                }
            }
            UIMgr.HandleMessage(UIMessage.MouseUp, args.X, args.Y);
        }
        /// <summary>
        /// 鼠标在窗口内滚轮消息Callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void MainWindow_MouseWheel(object sender, MouseEventArgs args)
        {
            if (MainGame.IsEditorMode)
            {
                if (args.Delta != 0)
                    CurrentScene.EditorOperate(EditorOp.Scale, args.Delta, args.Delta);
            }
        }
#endif
        /// <summary>
        /// 图形设备创建Callback，可以在此处修改图形设备的参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void Graphics_DeviceCreated(Object sender, EventArgs args)
        {
            GraphicsDeviceManager graphics = sender as GraphicsDeviceManager;
            graphics.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            CurrentScene = new Scene("scene1", 0, 0, GameConst.ScreenWidth, GameConst.ScreenHeight);
            //CurrentScene.ActualSize = new Vector4(0, 0, 2048 * GameConst.BackgroundScale, 2048 * GameConst.BackgroundScale);
            CurrentScene.ActualSize = new Vector4(0, 0, 20000, 2048 * GameConst.BackgroundScale);

            //init net client
            try
            {
                string ipaddress = "192.168.1.6";
                int port = 9610;
                using(System.IO.Stream stream = TitleContainer.OpenStream("servers.xml"))
                {
                    XDocument doc = XDocument.Load(stream);

                    XNode x = doc.NextNode;
                    XElement serverelement = doc.Element("Server");
                    if (serverelement != null)
                    {
                        ipaddress = serverelement.Element("Ip").Value;
                        port = int.Parse(serverelement.Element("Port").Value);
                    }
                    
                }
            

                clientchannel = new Beetle.ProtoBufAdapter.ProtoBufChannel();
                //必须调用这一句，生成message的查找字典，注意message放在哪个namespace下，就要从哪个里面来
                //load assembly
                ProjectXServer.Messages.ProtobufAdapter.LoadMessage(Assembly.Load("ProjectXServer.MessagesWP7"));
                clientchannel.Receive += new EventChannelReceive(ReceiveMessage);
                clientchannel.Disposed += new EventChannelDisposed(channel_ChannelDisposed);
                clientchannel.Error += new EventChannelError(clientchannel_Error);
                clientchannel.Connected += new EventChannelConnected(clientchannel_Connected);
                clientchannel.Completed += new EventSendCompleted(clientchannel_Completed);
                clientchannel.Connect(ipaddress, port);
            }
            catch (Exception e_)
            {
                
            }
            base.Initialize();
        }

        protected void LoadContentThread()
        {
            try
            {
                //token.ThrowIfCancellationRequested();
                mainfont = Content.Load<SpriteFont>(@"font/YaHeiCh16");
                GameConst.CurrentFont = mainfont;

                UIMgr.ControlsTexture = Content.Load<Texture2D>(@"ui/controls");

                CharacterTitle.TakeQuestTexture = Content.Load<Texture2D>(@"questicon/take");
                CharacterTitle.QuestCompletedTexture = Content.Load<Texture2D>(@"questicon/done");
                CharacterTitle.QuestNonCompletedTexture = Content.Load<Texture2D>(@"questicon/notdone");


                // load cloud texture
                for (int i = 0; i < 15; ++i)
                {
                    cloudTextureArray[i] = Content.Load<Texture2D>(@"cloud/yun_b" + string.Format("{0:d2}", i));
                }

                //init character
                Content.Load<CharacterDefinition.PicDef>(@"chardef/char3");
                CurrentScene.LoadGameData();
                CurrentScene.LoadBackground();


                Texture2D texminimap = Content.Load<Texture2D>(@"minimap/scene1");
                Texture2D texminimapchar = Content.Load<Texture2D>(@"minimap/charactericon");
                Texture2D texmapmask = Content.Load<Texture2D>(@"minimap/mapmask");
                CurrentScene.InitMiniMap(texminimap, texminimapchar, texmapmask, 0, GameConst.ScreenHeight - 256, 256, 256);

                CharacterTitle.BlockTexture = Content.Load<Texture2D>(@"effect/block");
                UIMgr.AddUIControl("Dialog_Leader", "leader_dlg", (int)UILayout.Right, (int)UILayout.Top, 0, 0, -1, 99, this);
                CurrentScene.GenerateClouds(cloudTextureArray);
                CurrentScene.SortRenderChunksByLayer();

                spritebatchrenderer.GraphicsDeviceService = this.graphics;
                spritebatchrenderer.LoadContent(null);

                peTrails = GameConst.Content.Load<ParticleEffect>(@"particles/magictrail");
                for (int i = 0; i < peTrails.Emitters.Count; i++)
                {
                    peTrails.Emitters[i].ParticleTexture = GameConst.Content.Load<Texture2D>(@"particles/" + peTrails.Emitters[i].ParticleTextureAssetPath);
                    peTrails.Emitters[i].Initialise();
                }

                peSpawn = GameConst.Content.Load<ParticleEffect>(@"particles/BeamMeUp");
                for (int i = 0; i < peSpawn.Emitters.Count; i++)
                {
                    peSpawn.Emitters[i].ParticleTexture = GameConst.Content.Load<Texture2D>(@"particles/" + peSpawn.Emitters[i].ParticleTextureAssetPath);
                    peSpawn.Emitters[i].Initialise();
                }

                peClick = GameConst.Content.Load<ParticleEffect>(@"particles/BasicExplosion");
                for (int i = 0; i < peClick.Emitters.Count; i++)
                {
                    peClick.Emitters[i].ParticleTexture = GameConst.Content.Load<Texture2D>(@"particles/" + peClick.Emitters[i].ParticleTextureAssetPath);
                    peClick.Emitters[i].Initialise();
                }

                Thread.Sleep(10);
                ContentLoadCompleted = true;
            }
            catch (ContentLoadException e)
            {
                throw new ContentLoadException("载入资源发生错误，程序关闭: " + e.Message);
            }
        }
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //spritebatchrenderer = new SpriteBatchRenderer();
            spritebatchrenderer = new SpriteBatchRenderer
            {
                GraphicsDeviceService = this.graphics,
                Transformation = Matrix.CreateTranslation(0, 0, 1f)
            };

            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, GameConst.ScreenWidth, GameConst.ScreenHeight);

            loadingTexture = Content.Load<Texture2D>("ui/loading");

            Thread thread_loadc = new Thread(new ThreadStart(LoadContentThread));
            thread_loadc.Start();
                
            if (clientchannel != null && !clientchannel.Disconnected)
            {
                MainGame.LoginToServer("test1", "69-8D-51-A1-9D-8A-12-1C-E5-81-49-9D-7B-70-16-68");
            }
            else
            {
                ProjectXServer.Messages.PlayerLoginSelfMsg msg = new ProjectXServer.Messages.PlayerLoginSelfMsg();
                msg.ClientID = 0;
                msg.Name = "player1";
                msg.Position = new float[] { GameConst.ScreenWidth / 2, GameConst.ScreenHeight / 2 };
                msg.Speed = GameConst.PlayerSpeed;
                msg.ATK = 500;
                msg.DEF = 30;
                msg.HP = 1000;
                msg.MaxHP = 1000;

                Thread thread_createlp = new Thread(new ParameterizedThreadStart(CreateLocalPlayer));
                thread_createlp.Start(msg);
                //CreateLocalPlayer(msg);
            }
        }
        /// <summary>
        /// 删除角色实例
        /// </summary>
        /// <param name="pn">角色属性描述</param>
        private void DestoryPlayer(ProjectXServer.Messages.PlayerLogoutMsg pn)
        {
            NetPlayer p = CurrentScene.FindNetPlayer(pn.ClientID);
            if (p != null)
            {
                if (CurrentScene.State == Scene.SceneState.Map)
                {
                    p.Picture.State = RenderChunk.RenderChunkState.FadeOutToDel;
                    p.Title.State = RenderChunk.RenderChunkState.FadeOutToDel;
                }
                else
                {
                    p.Picture.State = RenderChunk.RenderChunkState.Delete;
                    p.Title.State = RenderChunk.RenderChunkState.Delete;
                }
                CurrentScene.DelNetPlayer(p);
            }

        }
        /// <summary>
        /// 创建本地Player角色
        /// </summary>
        /// <param name="pn">角色创建属性msg，描述角色基本属性</param>
        private void CreateLocalPlayer(/*ProjectXServer.Messages.PlayerLoginSelfMsg pn*/object o)
        {
            while (!ContentLoadCompleted);
            ProjectXServer.Messages.PlayerLoginSelfMsg pn = o as ProjectXServer.Messages.PlayerLoginSelfMsg;
            player = new Player(pn.Name, CurrentScene);
            CharacterDefinition.PicDef pd = Content.Load<CharacterDefinition.PicDef>(@"chardef/char3");
            CharacterPic cpic = new CharacterPic(pd, 15);
            player.Picture = cpic;
            CharacterTitle title = new CharacterTitle(GameConst.CurrentFont);
            title.Layer = 15;
            title.NameString = pn.Name;
            title.Character = player;
            player.Title = title;
            player.Position = new Vector2(pn.Position[0], pn.Position[1]);
            player.Speed = pn.Speed;//GameConst.PlayerSpeed;
            player.ATK = pn.ATK;//GameConst.PlayerAtk;
            player.DEF = pn.DEF;//GameConst.PlayerAtk;
            player.HP = pn.HP;//GameConst.PlayerHP;
            player.MaxHP = pn.MaxHP;// GameConst.PlayerHP;
            //player.AddActionSet("Idle", CharacterState.Spawn, CharacterActionSetChangeFactor.EffectCompleted, "Spawn");
            if (peSpawn != null)
            {
                Vector3 p3 = new Vector3(pn.Position[0], pn.Position[1], 0);
                peSpawn.Trigger(ref p3);
            }

            player.AddActionSet("Idle", CharacterState.Spawn, CharacterActionSetChangeFactor.Time, 2.0);
            player.AddActionSet("Idle", CharacterState.Idle, CharacterActionSetChangeFactor.Immediate, null);
            player.ClientID = pn.ClientID;
            CurrentScene.AddCharacter(player);
            CurrentScene.Player = player;
            player.TrailParticle = peTrails;
            player.UpdateSceneScroll();
           
        }
        /// <summary>
        /// 创建远端Player实例
        /// </summary>
        /// <param name="pn">远端角色属性描述msg</param>
        private void CreatePlayer(ProjectXServer.Messages.PlayerLoginMsg pn)
        {
            while (!ContentLoadCompleted) ;
            NetPlayer playernet = new NetPlayer(pn.Name, CurrentScene);
            CharacterDefinition.PicDef pd = Content.Load<CharacterDefinition.PicDef>(@"chardef/char3");
            CharacterPic cpic = new CharacterPic(pd, 15);
            playernet.Picture = cpic;
            CharacterTitle title = new CharacterTitle(GameConst.CurrentFont);
            title.Layer = 15;
            title.NameString = pn.Name;
            title.Character = playernet;
            playernet.Title = title;
            playernet.Position = new Vector2(pn.Position[0], pn.Position[1]);
            playernet.Speed = pn.Speed;// GameConst.PlayerSpeed;
            playernet.ATK = pn.ATK;//GameConst.PlayerAtk;
            playernet.DEF = pn.DEF;//GameConst.PlayerAtk;
            playernet.HP = pn.HP;//GameConst.PlayerHP;
            playernet.MaxHP = pn.MaxHP;// GameConst.PlayerHP;
            //playernet.AddActionSet("Idle", CharacterState.Spawn, CharacterActionSetChangeFactor.EffectCompleted, "Spawn");
            playernet.AddActionSet("Idle", CharacterState.Idle, CharacterActionSetChangeFactor.Immediate, null);
            playernet.ClientID = pn.ClientID;
            //playernet.AddPreRenderEffect("Spawn", spawnEffect);
            CurrentScene.AddNetPlayer(playernet);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param _name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();
            if (!ContentLoadCompleted)
                return;
            UpdateInput();
            InterpolatioAnimationMgr.Update(gameTime);
            CurrentScene.Update(gameTime);
            spritebatchrenderer.Transformation = Matrix.CreateTranslation(-CurrentScene.Viewport.X, -CurrentScene.Viewport.Y, 0);
            if (peSpawn != null)
            {
                peSpawn.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            if (peTrails != null)
            {
                peTrails.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            if (peClick != null)
            {
                peClick.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            UIMgr.Update(gameTime);
            //player.Update(gameTime);


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param _name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        Matrix idmatrix = new Matrix();
        float _loadingangle = 0;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            if (!ContentLoadCompleted)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(loadingTexture, new Vector2(GameConst.ScreenWidth - loadingTexture.Width, GameConst.ScreenHeight - loadingTexture.Height),
                                  null, Color.White, _loadingangle, new Vector2(loadingTexture.Width / 2, loadingTexture.Height / 2), 1.0f, SpriteEffects.None, 1.0f);
                spriteBatch.End();
                _loadingangle += 0.1f;
                return;
            }

            GameConst.RenderCountPerFrame = 0;
            CurrentScene.RenderPrepositive();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            CurrentScene.Render(spriteBatch);
            spriteBatch.End();
            CurrentScene.RenderPostpositive();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            UIMgr.Render(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            if (player != null)
                spriteBatch.DrawString(mainfont, string.Format("{0:d}, {1:d} {2:d}", (int)player.Position.X, (int)player.Position.Y, GameConst.RenderCountPerFrame), Vector2.Zero, Color.Red);
            spriteBatch.End();

            //idmatrix
            Vector3 p = new Vector3();
            if (peTrails != null)
                spritebatchrenderer.RenderEffect(peTrails, ref idmatrix, ref idmatrix, ref idmatrix, ref p);
            if (peClick != null)
                spritebatchrenderer.RenderEffect(peClick, ref idmatrix, ref idmatrix, ref idmatrix, ref p);
            if (peSpawn != null)
                spritebatchrenderer.RenderEffect(peSpawn, ref idmatrix, ref idmatrix, ref idmatrix, ref p);
            base.Draw(gameTime);
        }


        HiPerfTimer clicktimer;
        double clicktime = 0;
        Vector3 clickPos = new Vector3();
        /// <summary>
        /// 鼠标左键单击消息响应
        /// </summary>
        /// <param name="cx">鼠标指针x坐标</param>
        /// <param name="cy">鼠标指针y坐标</param>
        private void OnMouseLeftClick(int cx, int cy)
        {
            if (CurrentScene.State == Scene.SceneState.Map)
            {
                if (clicktimer == null)
                {
                    clicktimer = new HiPerfTimer();
                    clicktimer.Start();
                }

                clicktime = clicktimer.GetTotalDuration();

                if (clicktime > 0.2 && (player.State == CharacterState.Idle || player.State == CharacterState.Moving))
                {
                    clicktimer.Stop();
                    clicktimer.Start();
                    int sresult = CurrentScene.SelectCharacter();
                    if (sresult == 0)
                    {
                        clickPos.X = CurrentScene.Viewport.X + cx;
                        clickPos.Y = CurrentScene.Viewport.Y + cy;
                        if (peClick != null)
                            peClick.Trigger(ref clickPos);

                        player.Target = new Vector2(CurrentScene.Viewport.X + cx, CurrentScene.Viewport.Y + cy);
                        if (player.State == CharacterState.Idle)
                        {
                            player.AddActionSet("Launch", CharacterState.Launch, CharacterActionSetChangeFactor.AnimationCompleted, null);
                            player.AddActionSet("Moving", CharacterState.Moving, CharacterActionSetChangeFactor.ArriveTarget, player.Target);
                            player.AddActionSet("Landing", CharacterState.Landing, CharacterActionSetChangeFactor.AnimationCompleted, null);
                            player.AddActionSet("Idle", CharacterState.Idle, CharacterActionSetChangeFactor.Immediate, null);
                            if (ClientID != 0)
                            {
                                player.StartMoveSyncTimer();
                                SendRequestMovementMsg(player);
                            }
                        }
                        else
                        {
                            if (ClientID != 0)
                                SendTargetChangedMsg(player);
                        }
                        if (!player.Interacting)
                            player.InteractiveTarget = null;
                    }
                    else if (sresult == 1) //选中其他角色
                    {
                        if (player.State == CharacterState.Idle)
                        {
                            if (ClientID != 0)
                            {
                                player.StartMoveSyncTimer();
                                SendRequestMovementMsg(player);
                            }
                        }
                        else
                        {
                            if (ClientID != 0)
                                SendTargetChangedMsg(player);
                        }
                    }
                }
                else
                {
                    if ((player.State == CharacterState.Idle || player.State == CharacterState.Moving))
                    {
                        Debug.WriteLine("too fast to click");
                    }
                }
            }
            else if (CurrentScene.State == Scene.SceneState.Battle)
            {
                CurrentScene.ConfirmOperateTarget();
            }
        }

        [Flags]
        enum ScrollDir
        {
            None = 0,
            Up = 1,
            Down = 2,
            Left = 4,
            Right = 8,
        }
        /// <summary>
        /// 控制屏幕卷轴
        /// </summary>
        /// <param name="dir">卷轴方向</param>
        private void HandleScreenScroll(ScrollDir dir)
        {
            Vector4 vp = CurrentScene.Viewport;
            bool isscrolled = false;//是否需要更新viewport
            if (CurrentScene.State == Scene.SceneState.Map)
            {
                if (MainGame.IsEditorMode)
                {
                    if (((int)dir & (int)ScrollDir.Down) != 0)
                    {
                        vp.Y += GameConst.ScrollSpeed;
                        isscrolled = true;
                    }
                    if (((int)dir & (int)ScrollDir.Up) != 0)
                    {
                        vp.Y -= GameConst.ScrollSpeed;
                        isscrolled = true;
                    }
                    if (((int)dir & (int)ScrollDir.Left) != 0)
                    {
                        vp.X -= GameConst.ScrollSpeed;
                        isscrolled = true;
                    }
                    if (((int)dir & (int)ScrollDir.Right) != 0)
                    {
                        vp.X += GameConst.ScrollSpeed;
                        isscrolled = true;
                    }
                    if (isscrolled)
                    {
                        CurrentScene.Viewport = vp;
                        Debug_ClipScroll();
                    }

                }
            }
        }

        
        KeyboardState _ksLast = new KeyboardState();
        /// <summary>
        /// 更新输入， 跟消息响应机制不同， 本方法每帧采样一次， 来判断输入状态
        /// 对应没有消息循环机制的平台，但当framerate比较低的时候，会有输入
        /// 响应不及时的问题
        /// </summary>
        public void UpdateInput()
        {
            if (player == null)
                return;

            TouchCollection TouchState = TouchPanel.GetState();

            if (TouchState.Count > 0 && TouchState[0].State == TouchLocationState.Pressed)
            {
                int result = UIMgr.HandleMessage(UIMessage.MouseClick, TouchState[0].Position.X, TouchState[0].Position.Y);
                if (result != 0)
                    return;
                if (!MainGame.IsEditorMode)
                {
                    CurrentScene.HighLightCharacterByPoint((int)TouchState[0].Position.X, (int)TouchState[0].Position.Y);
                    OnMouseLeftClick((int)TouchState[0].Position.X, (int)TouchState[0].Position.Y);
                }
            }
        }


        /// <remarks>Debug Only</remarks>
        public void Debug_ClipScroll()
        {
            Vector4 vp = CurrentScene.Viewport;
            //vp.X = MathHelper.Clamp(vp.X, 0.0f, (GameConst.BackgroundScale) * 2048.0f - GameConst.ScreenWidth);
            //vp.Y = MathHelper.Clamp(vp.Y, 0.0f, (GameConst.BackgroundScale) * 2048.0f - GameConst.ScreenHeight);
            vp.X = MathHelper.Clamp(vp.X, 0.0f, CurrentScene.ActualSize.Z);
            vp.Y = MathHelper.Clamp(vp.Y, 0.0f, CurrentScene.ActualSize.W - GameConst.ScreenHeight);

            CurrentScene.Viewport = vp;
        }
    }
}
