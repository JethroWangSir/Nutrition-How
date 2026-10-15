using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ship4 : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 horizontalMove,verticalMove;
    public bool facing_right=true;
    public float speed,force;
    public float hurtforce;
    public float timeLeft;
    public Text timeText;
    public GameObject timeTextObject;
    public GameObject loseDialog;
    public GameObject door;
    public GameObject[] timeImage;
    public bool passLevel;
    private PlayerInputActions controls;
    public GameObject water;
    private bool timerExpired;
    private bool sceneTransitionScheduled;
    private bool victoryFeedbackPlayed;
    private bool failureFeedbackPlayed;
    private int growthStage;
    private int displayedSeconds=int.MinValue;
    private int countdownImageIndex=int.MinValue;
    private void Awake()
    {
        controls = new PlayerInputActions();
        controls.Player.Move.performed += ctx => horizontalMove = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => horizontalMove = Vector2.zero;
        controls.Player.Move.performed += ctx => verticalMove = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => verticalMove = Vector2.zero;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        controls.Player.GoGo.started += ctx => GoGo();
#endif
    }
    void OnEnable()
    {
        controls.Player.Enable();
    }
    void OnDisable()
    {
        controls.Player.Disable();
    }
    void OnDestroy()
    {
        if(controls != null)
        {
            controls.Dispose();
            controls = null;
        }
    }

    void Start()
    {
        rb=GetComponent<Rigidbody2D>();
        anim=GetComponent<Animator>();
        timerExpired=false;
        growthStage=0;
        // Time.timeScale=1f;
    }

    void FixedUpdate()
    {
        if(timeLeft>0f)
        {
            Movement();
        }
    }
    void Update()
    {
        Timer();
        Bigger();
    }

    void GoGo()
    {
        this.gameObject.transform.position=new Vector2(water.transform.position.x,water.transform.position.y);
    }

    void Timer()
    {
        int roundedSeconds=Mathf.RoundToInt(timeLeft);
        if(displayedSeconds!=roundedSeconds)
        {
            displayedSeconds=roundedSeconds;
            timeText.text=roundedSeconds.ToString();
        }
        if(timeLeft>0f)
        {
            timeLeft-=Time.deltaTime;
            UpdateCountdownImage(GetCountdownImageIndex(timeLeft));
        }else
        {
            timeLeft=0f;
            if(!timerExpired)
            {
                timerExpired=true;
                BoxCollider2D waterCollider=water.GetComponent<BoxCollider2D>();
                if(waterCollider!=null)
                {
                    waterCollider.enabled=false;
                }
                if(!failureFeedbackPlayed)
                {
                    failureFeedbackPlayed=true;
                    if(soundmanager.sound_instance!=null)
                    {
                        soundmanager.sound_instance.FailureAudio();
                    }
                    GameplayPlaytestMetrics.FailActiveLevel("time_expired");
                }
                loseDialog.SetActive(true);
                Time.timeScale=0f;
            }
        }
    }

    int GetCountdownImageIndex(float seconds)
    {
        if(seconds<5.5f && seconds>=4.5f) return 4;
        if(seconds<4.5f && seconds>=3.5f) return 3;
        if(seconds<3.5f && seconds>=2.5f) return 2;
        if(seconds<2.5f && seconds>=1.5f) return 1;
        if(seconds<1.5f && seconds>=0.5f) return 0;
        return -1;
    }

    void UpdateCountdownImage(int activeIndex)
    {
        if(countdownImageIndex==activeIndex)
        {
            return;
        }
        countdownImageIndex=activeIndex;
        if(activeIndex>=0 && soundmanager.sound_instance!=null)
        {
            soundmanager.sound_instance.CounterAudio();
        }
        for(int i=0;i<timeImage.Length;i++)
        {
            timeImage[i].SetActive(i==activeIndex);
        }
    }

    void Movement()
    {
        // horizontalMove=Input.GetAxis("Horizontal");
        // verticalMove=Input.GetAxis("Vertical");
        if(horizontalMove.x!=0f)
        {
            // rb.velocity=new Vector2(horizontalMove.x*speed,rb.velocity.y);
            rb.AddForce(new Vector2(horizontalMove.x*force, 0f),ForceMode2D.Force);
            if(Mathf.Abs(rb.linearVelocity.x) >= Mathf.Abs(horizontalMove.x*speed))
            {
                rb.linearVelocity=new Vector2(horizontalMove.x*speed,rb.linearVelocity.y);
            }
        }
        if(horizontalMove.x>0f&&!facing_right)
        {
            Flip();
        }else if(horizontalMove.x<0f&&facing_right)
        {
            Flip();
        }
        if(verticalMove.y!=0f)
        {
            // rb.velocity=new Vector2(rb.velocity.x,verticalMove.y*speed);
            rb.AddForce(new Vector2(0f, verticalMove.y*force),ForceMode2D.Force);
            if(Mathf.Abs(rb.linearVelocity.y) >= Mathf.Abs(verticalMove.y*speed))
            {
                rb.linearVelocity=new Vector2(rb.linearVelocity.x,verticalMove.y*speed);
            }
        }
    }

    void Flip()
    {
        facing_right=!facing_right;
        transform.Rotate(0f, 180f, 0f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag=="water4-1" && !sceneTransitionScheduled)
        {
            sceneTransitionScheduled=true;
            timeTextObject.SetActive(false);
            timeLeft=20;
            timerExpired=false;
            growthStage=0;
            soundmanager.sound_instance.SwimAudio();
            anim.Play("smaller");
            Invoke(nameof(NextShip1),0.8f);
            Invoke(nameof(NextScene),1.5f);
        }
        if(collision.tag=="water4-2")
        {
            timeTextObject.SetActive(false);
            timeLeft=20;
            timerExpired=false;
            growthStage=0;
            soundmanager.sound_instance.SwimAudio();
            anim.Play("smaller");
            Invoke(nameof(NextShip1),0.8f);
            if(!victoryFeedbackPlayed)
            {
                victoryFeedbackPlayed=true;
                if(soundmanager.sound_instance!=null)
                {
                    soundmanager.sound_instance.VictoryAudio();
                }
            }
            door.SetActive(true);
        }
        if(collision.tag=="door" && !sceneTransitionScheduled)
        {
            sceneTransitionScheduled=true;
            soundmanager.sound_instance.DoorAudio();
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
            passLevel=true;
            anim.Play("fadeout");
            Invoke(nameof(GotoMenu),0.8f);
        }
    }

    void GotoMenu()
    {
        GameObject.Find("levelLoader").GetComponent<levelLoader>().LoadSomeLevel(NutritionHowScenes.Menu);
    }

    void NextScene()
    {
        GameObject.Find("levelLoader").GetComponent<levelLoader>().LoadNextLevel();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag=="wall")
        {
            soundmanager.sound_instance.CrackAudio();
            if (collision.contacts[0].normal.y == -1)//从上方碰撞
            {
                rb.AddForce(new Vector2(0f,-hurtforce),ForceMode2D.Impulse);
            }
            else if(collision.contacts[0].normal.y == 1)//从下方碰撞
            {
                rb.AddForce(new Vector2(0f,hurtforce),ForceMode2D.Impulse);
            }
            else if (collision.contacts[0].normal.x == -1)//左边碰撞
            {
                rb.AddForce(new Vector2(-hurtforce,0f),ForceMode2D.Impulse);
            }
            else if (collision.contacts[0].normal.x == 1)//右边碰撞
            {
                rb.AddForce(new Vector2(hurtforce,0f),ForceMode2D.Impulse);
            }
        }
    }

    void Bigger()
    {
        if(timeLeft>0f)
        {
            if(growthStage==0 && timeLeft<=15f)
            {
                growthStage=1;
                soundmanager.sound_instance.BiggerAudio();
                anim.Play("bigger");
                Invoke(nameof(NextShip2),0.8f);
            }
            else if(growthStage==1 && timeLeft<=10f)
            {
                growthStage=2;
                soundmanager.sound_instance.BiggerAudio();
                anim.Play("bigger2");
                Invoke(nameof(NextShip3),0.8f);
            }
            else if(growthStage==2 && timeLeft<=5f)
            {
                growthStage=3;
                soundmanager.sound_instance.BiggerAudio();
                anim.Play("bigger3");
                Invoke(nameof(NextShip4),0.8f);
            }
        }
    }
    void NextShip1()
    {
        anim.Play("idle");
    }
    void NextShip2()
    {
        anim.Play("idle2");
    }
    void NextShip3()
    {
        anim.Play("idle3");
    }
    void NextShip4()
    {
        anim.Play("idle4");
    }

    public bool Pass()
    {
        if(passLevel)
        {
            return true;
        }else
        {
            return false;
        }
    }
}
