using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ship : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim,collectionAnim,hpAnim,winAnim;
    public bool ishurt=false;
    private Vector2 horizontalMove;
    public float speed;
    public float accelerate;
    public float decelerate;
    public float jumpForce;
    public float hurtForce;
    public int collections;
    public Text collectionsnum;
    public int hp;
    public int max_hp;
    public GameObject hp1_red,hp1,hp2,hp3,hp4,hp5;
    public float invincibleTime;
    public float invincibleTimer;
    static public bool isInvincible;
    private float damagePulseElapsed;
    private SpriteDamageFeedback damageFeedback;
    public GameObject loseDialog;
    public bool passLevel;
    public GameObject door;
    private PlayerInputActions controls;
    private bool loseScheduled;
    private bool sceneTransitionScheduled;
    private bool winAnimationPlayed;
    private bool failureFeedbackPlayed;
    private string pendingFailureReason="health_depleted";
    private int displayedHp=int.MinValue;
    private int displayedCollections=int.MinValue;
    private void Awake()
    {
        controls = new PlayerInputActions();
        controls.Player.Move.performed += ctx => horizontalMove = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => horizontalMove = Vector2.zero;
        controls.Player.Jump.performed += ctx => Jump();
        controls.Player.Jump.canceled += ctx => JumpCancel();
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
        damageFeedback?.Restore();
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
        hp = max_hp;
        isInvincible=false;
        damageFeedback=new SpriteDamageFeedback(gameObject);
        collections=0;
        collectionAnim=GameObject.Find("collectionsnum").GetComponent<Animator>();
        hpAnim=GameObject.Find("hpbar_bottom").GetComponent<Animator>();
        winAnim=GameObject.Find("win").GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if(hp>0)
        {
            Movement();
            // Jump();
        }
    }

    void Update()
    {
        Hpfunction();
        Collectionsnum();
        Invinsible();
    }

    void GoGo()
    {
        CancelInvoke(nameof(Lose));
        loseScheduled=false;
        hp=max_hp;
        collectionAnim.SetTrigger("plus");
        collections+=10;
        this.gameObject.transform.position=new Vector2(78f,35f);
    }

    void Movement()
    {
        if(horizontalMove.x!=0f)
        {
            if(horizontalMove.x>0f)
            {
                rb.linearVelocity=new Vector2(accelerate*speed*Time.fixedDeltaTime,rb.linearVelocity.y);
            }else if(horizontalMove.x<0f)
            {
                rb.linearVelocity=new Vector2(decelerate*speed*Time.fixedDeltaTime,rb.linearVelocity.y);
            }
        }else
        {
            rb.linearVelocity=new Vector2(speed*Time.fixedDeltaTime,rb.linearVelocity.y);
        }
    }

    void Jump()
    {
        rb.AddForce(new Vector2(0f,jumpForce),ForceMode2D.Impulse);
    }
    void JumpCancel()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
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

    void Lose()
    {
        hp=0;
        if(!failureFeedbackPlayed)
        {
            failureFeedbackPlayed=true;
            if(soundmanager.sound_instance!=null)
            {
                soundmanager.sound_instance.FailureAudio();
            }
            GameplayPlaytestMetrics.FailActiveLevel(pendingFailureReason);
        }
        loseDialog.SetActive(true);
        Time.timeScale=0f;
    }

    void ScheduleLose(string reason="health_depleted")
    {
        if(loseScheduled)
        {
            return;
        }
        loseScheduled=true;
        pendingFailureReason=reason;
        Invoke(nameof(Lose),1.5f);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag=="bosscorner")
        {
            if(collections<=7)
            {
                ScheduleLose("objective_requirement");
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag=="enemy")
        {
            Hurt();
            if(collision.gameObject.transform.position.y>this.transform.position.y)
            {
                rb.AddForce(new Vector2(0f,-hurtForce),ForceMode2D.Impulse);
            }else if(collision.gameObject.transform.position.y<this.transform.position.y)
            {
                rb.AddForce(new Vector2(0f,hurtForce),ForceMode2D.Impulse);
            }
        }
    }

    void Hurt()
    {
        if(!isInvincible)
        {
            ishurt=true;
            soundmanager.sound_instance.Hurtaudio();
            hp-=1;
        }
        Startinvincible();
    }

    void Hpfunction()
    {
        if(hp>=5)
        {
            hp=5;
        }

        if(displayedHp==hp)
        {
            return;
        }
        displayedHp=hp;

        if(hp>=5)
        {
            hpAnim.Play("idle");
        }
        else if(hp==4)
        {
            hpAnim.Play("hp5");
        }
        else if(hp==3)
        {
            hpAnim.Play("hp4");
        }
        else if(hp==2)
        {
            hpAnim.Play("hp3");
        }
        else if(hp==1)
        {
            hpAnim.Play("hp2");
        }
        else
        {
            hpAnim.Play("hp1");
            ScheduleLose("health_depleted");
        }
    }

    void Collectionsnum()
    {
        if(displayedCollections!=collections)
        {
            displayedCollections=collections;
            collectionsnum.text=collections.ToString();
        }
        if(collections>=8 && hp>=1)
        {
            if(!winAnimationPlayed)
            {
                winAnimationPlayed=true;
                door.SetActive(true);
                if(soundmanager.sound_instance!=null)
                {
                    soundmanager.sound_instance.VictoryAudio();
                }
                if(winAnim!=null)
                {
                    winAnim.Play("win");
                }
            }
        }
    }

    public void CollectionsPlus()
    {
        collectionAnim.SetTrigger("plus");
        collections+=1;
    }

    void Startinvincible()
    {
        if(isInvincible)
        {
            return;
        }
        isInvincible=true;
        invincibleTimer=invincibleTime;
        damagePulseElapsed=0f;
        damageFeedback?.Restore();
    }
    void Invinsible()
    {
        if(isInvincible)
        {
            invincibleTimer-=Time.deltaTime;
            damagePulseElapsed+=Time.deltaTime;
            damageFeedback?.Pulse(damagePulseElapsed);
            if (invincibleTimer<=0f)
            {
                damageFeedback?.Restore();
                isInvincible=false;
            }
        }
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
