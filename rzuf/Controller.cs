using SFML.Window;
using SFML.Graphics;
using SFML.System;
using SFML.Audio;
using System.Runtime.Versioning;
using System.Net.Mail;

namespace Sim
{
    class Controller
    {   
        //variables for window
        public RenderWindow window; //de window
        VideoMode videoMode; //de resolution
        uint frameRate; //de framerate

        //game variables
        int width,height; //width and height of window for easier use
        public int enemyCount = 0; //how many enemies are on screen

        public int enemiesToKill;
        float closestEnemyDistance = 9999; 
        int turn = 0; //number of turns to calculate enemies' stats
        bool turnEnd = false;

        bool spawningEnemies = false;

        //game objects

        public static List<Creature> enemies = new List<Creature>(); //list of enemies
        public static Rzuf rzuf; //player
        Random losu = new Random(); //random bullshit generator
        Soldier closestEnemy; //it is temporary Soldier object to make easier for rzuf to shoot to

         public Sprite background = new Sprite(); //spaaaaaaaace

        static public List<Sound> sounds = new List<Sound>(); //list of current sounds because SFML momento

        static public List<Text> gui = new List<Text>(); //list of strings of gui

        //functions
        public bool Running() //checking if window is open
        {
            return this.window.IsOpen;
        }
        public void HandleClose(object sender, EventArgs e) //closing the window
            {
                this.window.Close();
                
            }
        //creating objects
        public void SpawnPlayer(int _maxHP, int _damage, int _attackDelay, int _maxAmmo) //spawns rzuf object
        {   
            //creates rzuf object
            rzuf = new Rzuf(_maxHP, _damage, _attackDelay, _maxAmmo);
            rzuf.position = new Vector2f(this.videoMode.Width/2-50,this.videoMode.Height/2-50);
            TextureLibrary.SetSprite("rzuf", rzuf);
            rzuf.sprite.Position = rzuf.position;
            rzuf.gun.position = new Vector2f(this.videoMode.Width/2,this.videoMode.Height/2+25);
            TextureLibrary.SetTexture("gun", rzuf.gun.sprite);
            rzuf.gun.sprite.Position = rzuf.gun.position;
            rzuf.gun.sprite.Origin = new Vector2f(25f,25f);
        }
        public void CreateGui()
        {
            TextLibrary.WriteText("Tura: "+turn,width/2-200,10,gui); //turn
            TextLibrary.WriteText("Ilość wrogów w fali: "+enemiesToKill,width/2-50,10,gui); //number of enemies to end turn
            TextLibrary.WriteText("HP Rzuf: "+rzuf.currentHP+"/"+rzuf.maxHP,width/2-250,50,gui); //current hp of rzuf
            TextLibrary.WriteText("Amunicja Rzuf: "+rzuf.gun.currentAmmo+"/"+rzuf.gun.maxAmmo,width/2,50,gui); //current ammo of rzuf
            TextLibrary.WriteText("Poziom Rzuf: "+rzuf.lv,width/2-250,90,gui); //current rzuf level
            TextLibrary.WriteText("DMG Rzuf: "+rzuf.gun.damage,width/2,90,gui); //current damage of rzuf

        }
        public async void SpawnEnemies(int number, int chanceSoldier, int chanceTurret, int chanceArmoredSoldier, int chanceAngrySoldier)
        {   
            /*
            randomly fills enemies list with random enemy types and sets their sprites
            first parameter: number of enemies
            second parameter: chance for spawning Soldier (in %)
            third parameter: chance for spawning Turret (in %)
            forth parameter: chance for spawning Armored Soldier (in %)
            fifth parameter: chance for spawning Angry Soldier (in %)
            */
            spawningEnemies = true; //without this when first soldier is spawned and killed before spawning next, the next turn was starting
            if(turnEnd==true)
            {
                int losulosu; 
                for(int i = 0; i<number; i++)
                {   
                    losulosu = losu.Next(0,100); //gets random number from 0 to 99
                    if(losulosu>=0&&losulosu<chanceSoldier) 
                    { //spawns basic soldier and waits 0.5sec
                        Soldier soldier = new Soldier(turn, width, height);
                        TextureLibrary.SetSprite("soldier",soldier);
                        enemies.Add(soldier);
                    }
                    if(losulosu>=chanceSoldier&&losulosu<chanceSoldier+chanceTurret)
                    {  //spawns turret and waits 0.5sec
                        Soldier soldier = new Turret(turn, width, height);
                        TextureLibrary.SetSprite("turret",soldier);
                        enemies.Add(soldier);
                    }
                    if(losulosu>=chanceSoldier+chanceTurret&&losulosu<chanceSoldier+chanceTurret+chanceArmoredSoldier)
                    {  //spawns armored soldier and waits 0.5sec
                        Soldier soldier = new ArmoredSoldier(turn, width, height);
                        TextureLibrary.SetSprite("armored soldier",soldier);
                        enemies.Add(soldier);
                    }
                    if(losulosu>=chanceSoldier+chanceTurret+chanceArmoredSoldier&&losulosu<=100)
                    {  //spawns angry soldier and waits 0.5sec
                        Soldier soldier = new AngrySoldier(turn, width, height);
                        TextureLibrary.SetSprite("angry soldier",soldier);
                        enemies.Add(soldier);

                    }
                    

                    enemyCount++;
                    await Task.Delay(500); //maybe pass time between enemies spawning as argument?

                }
            }
            spawningEnemies = false;
        }
        public void StartTurn() //starts next turn
        {   
            if(turnEnd == true && rzuf.alive == true) 
            {   
                turn++;
                enemiesToKill = turn*5;
                SpawnEnemies(enemiesToKill,25,25,25,25); //number of enemies, chance for soldier, turret, armored, angry
                rzuf.Heal(0.5);
                rzuf.LvUp();
                turnEnd = false;
            }
        }
        //game logic
        void UpdatePlayer()
        { 
            foreach(Soldier soldier in enemies) //checks which enemy is closest to rzuf 
            {
                if(soldier.distance<closestEnemyDistance)
                {
                closestEnemyDistance = soldier.distance;
                closestEnemy = soldier;
                }
            }
            if(enemyCount!=0 &&rzuf.alive == true) //so rzuf cannot shot when there's no enemies on screen
            {rzuf.Act(closestEnemy); //shoots closest enemy
            float degrees = Utility.GetAngle(closestEnemy.position,rzuf.gun.position);
            rzuf.gun.sprite.Rotation = degrees;
            }
            if(enemyCount ==0 && spawningEnemies == false)
                turnEnd = true; //rzuf is always on screen, so he can control if all enemies are dead and next turn can be started
        }
        void UpdateEnemies()
        {
                //moving enemies
            foreach(Soldier soldier in enemies.ToList()) 
            {   if(soldier.alive == true && rzuf.alive == true) //hmmm not the intended way, but when rzuf is dead all the enemies are cleared
                    soldier.Act(rzuf);
                else    //deletes enemy from list when they're dead
                {
                    enemies.Remove(soldier);
                    enemiesToKill--;
                    enemyCount--;
                    closestEnemyDistance = 9999;
                }
            }
        }
        void UpdateSounds() //sound in SFML are stupid, so this function deletes all stopped sounds so computer not explode XD
        {  SoundStatus status;
            foreach(Sound sound in sounds.ToList())
            {   
                status = sound.Status;
                if(status==SoundStatus.Stopped)
                {
                    sounds.Remove(sound);
                }
            }
        }
        void UpdateGui()
        {
            foreach(Text line in gui)
            {
                if(line.DisplayedString.Contains("Tura")==true)
                    line.DisplayedString = "Tura: "+turn;
                if(line.DisplayedString.Contains("wrogów")==true)
                    line.DisplayedString = "Ilość wrogów w fali: "+enemiesToKill;
                if(line.DisplayedString.Contains("HP")==true)
                { 
                    if(rzuf.currentHP>=0)
                    line.DisplayedString = "HP Rzuf: "+rzuf.currentHP+"/"+rzuf.maxHP;
                    else
                    line.DisplayedString = "Rzuf is dead :( ";
                }
                if(line.DisplayedString.Contains("Amunicja")==true)
                {   
                    if(rzuf.gun.isReloading==false)
                    line.DisplayedString = "Amunicja Rzuf: "+rzuf.gun.currentAmmo+"/"+rzuf.gun.maxAmmo;
                    else
                    line.DisplayedString = "Amunicja Rzuf: przeładowuje";
                }
                if(line.DisplayedString.Contains("Poziom")==true)
                    line.DisplayedString = "Poziom rzuf: "+rzuf.lv;
                if(line.DisplayedString.Contains("DMG")==true)
                    line.DisplayedString = "DMG rzuf: "+rzuf.gun.damage;

            }
        }
        public void Update() //updates logic of game every frame
        {
            this.UpdateSounds();
            this.UpdateGui();
            this.UpdateEnemies();
            this.UpdatePlayer();

        }
        //game rendering
        public void SetBackground(string _type) 
        {
            TextureLibrary.SetTexture(_type,background);
        }
        void RenderWorld() //draws space background
        {
            this.window.Draw(background);
        }
        void RenderPlayer()  //draws player sprite
        {
            this.window.Draw(rzuf.sprite);
            this.window.Draw(rzuf.gun.sprite);
        }
        void RenderEnemies() //draws enemies sprites
        {  
            foreach(Creature soldier in enemies.ToList())
           {
            this.window.Draw(soldier.sprite);
           }
        }
        void RenderGui() //draws enemies sprites
        {  
            foreach(Text line in gui)
           {
            this.window.Draw(line);
           }
        }
        public void Render() // renders the game objects
        {
            /*
            - clear old frame
            - render objects
            - display frame in window
            */
            this.window.Clear();

            this.RenderWorld();
            this.RenderGui();
            this.RenderEnemies();
            this.RenderPlayer();

            this.window.Display();
        }
        //constructors
        public Controller(uint width, uint height, uint fps)
        {
            this.videoMode.Width = width;
            this.videoMode.Height = height;
            this.width = checked((int)width);
            this.height = checked((int)height);
            this.frameRate = fps;
            this.window = new RenderWindow(this.videoMode,"Rzuf!");
            this.window.SetFramerateLimit(frameRate);
        }
    }


    
}