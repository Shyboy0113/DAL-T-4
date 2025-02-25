using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game_Button : MonoBehaviour
{

    //카메라
    //public Camera camera;

    public Text Hp_text;

    int maxHp = 10;
    public int currentHp;

    //효과음
    AudioSource audioSource;
    public AudioClip[] Sound_Effect;

    private GameObject Music;

    //이동
    Rigidbody2D rigid_body;
    float power = 5000f;

    public Text direction;
    float dir = 1.0f;

    //먼지 생성
    public GameObject Dirt;

    bool F4_press = false;
    bool ALT_press = false;

    //정지 UI
    public bool isPause = false;
    public GameObject Esc_Canvas;
    public GameObject Retry_Canvas;

    //노트 제거
    public GameObject Note;

    //1,0으로 비트 다르게
    int Rhythm = 1;

    //애니메이션
    private Animator animator;
    private int IsTab;
    

    //투명도 조절
    private SpriteRenderer sprite;

    //맵
    public Tilemap tilemap;

    //색 경계선에서 폭발 안 일어나게
    public bool triggerOn;

    //충돌 일어났을 때 변수
    bool isCol = false;

    private void offTrigger() => triggerOn = false;
    private void onTrigger() { triggerOn = true; Debug.Log("트리거 킴"); }

    /// <summary>
    /// 텍스트 이동 방향
    /// </summary>
    private void Text_Direction()
    {     
        if ((dir == -1 && IsTab == 1) || (dir == 1 && IsTab == 0)) { direction.text = "<color=#FF0000>Up</color>"; }
        else if ((dir == -1 && IsTab == 0) || (dir == 1 && IsTab == 1)) { direction.text = "<color=#FF0000>Down</color>"; }
    }
    private void Set_Retry_UI()
    {
        Time.timeScale = 0;
        audioSource.clip = Sound_Effect[5];
        audioSource.Play();
        Retry_Canvas.SetActive(true);
        
    }

    private void Awake()
    {
        Time.timeScale = 1;
        Music = GameObject.Find("BackGroundMusic");
    }
    private void Start()
    {
        //안 넣으면 NullException 오류뜸
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        rigid_body = GetComponent<Rigidbody2D>();

        sprite = GetComponent<SpriteRenderer>();

        IsTab = 0;

        triggerOn = true;

        direction.text = "<color=#FF0000>Up</color>";

        currentHp = maxHp;

    }
    
    void Update()
    {
        if(currentHp <= 0)
        {
            isPause = true;
            Time.timeScale = 0;
            Retry_Canvas.SetActive(true);
        }

        Hp_text.text = "x" + currentHp.ToString();

        if (Input.GetKeyDown(KeyCode.Escape) )
        {
            if (isPause == false)
            {
                Time.timeScale = 0;
                audioSource.clip = Sound_Effect[4];
                audioSource.Play();
                Esc_Canvas.SetActive(true);
                isPause = true;
            }

            else {
                Time.timeScale = 1;
                Esc_Canvas.SetActive(false);
                isPause = false;
            }
            
        }

        if (Input.GetKeyDown(KeyCode.F4) && isPause == false && isCol == false)
        {
            F4_press = true;
            
        }
        else if (Input.GetKeyUp(KeyCode.F4) && isPause == false && isCol == false)
        {
            if(ALT_press == false)
            {
                GameObject dirt = Instantiate(Dirt);
                dirt.transform.position = transform.position;
                Destroy(dirt, 0.2f);
                        
                rigid_body.AddForce(power * new Vector2(1, dir));

                if (Rhythm % 2 == 1) audioSource.clip = Sound_Effect[0];
                else audioSource.clip = Sound_Effect[1];

                audioSource.Play();

                Rhythm++;
                Rhythm %=2;
                                                
            }            

            ALT_press = false;
            F4_press = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftAlt) && isPause == false && isCol == false)
        {
            ALT_press = true;

            if (F4_press == true)
            {
                GameObject dirt = Instantiate(Dirt);
                dirt.transform.position = transform.position;
                Destroy(dirt, 0.2f);

                dir *= (-1);
                rigid_body.AddForce(power * new Vector2(1, dir));

                if (Rhythm % 2 == 1) audioSource.clip = Sound_Effect[0];
                else audioSource.clip = Sound_Effect[1];

                audioSource.Play();

                Rhythm++;
                Rhythm %= 2;

                Text_Direction();

            }

            //position이랑 Translate랑 둘 다 충돌 판정이 안난다. 다른 방법을 찾아야함. -> addforce랑 velocity 필요
            //transform.position += new Vector3(1, dir, 0);
            // transform.Translate(Time.deltaTime * (new Vector3(1, dir, 0)));            

        }

        if (Input.GetKeyDown(KeyCode.Tab) && isPause == false && isCol == false) 
        {
            offTrigger();
            //Debug.Log("트리거 끔");

            //camera.transform.eulerAngles = new Vector3(0, 0, 180);

            rigid_body.AddForce(power * new Vector2(1, 0));
            if (Rhythm % 2 == 1) audioSource.clip = Sound_Effect[0];
            else audioSource.clip = Sound_Effect[1];

            audioSource.Play();

            Rhythm++;
            Rhythm %= 2;


            if (IsTab == 0)
            {
                animator.SetBool("isTab", true);
                IsTab = 1;
            }
            else if (IsTab == 1)
            {              
                    animator.SetBool("isTab", false);
                    IsTab = 0;
              
            }

            Text_Direction();

            Invoke(nameof(onTrigger), 0.1f);
            
        }


    }


    void OnTriggerStay2D(Collider2D tile)
    {
        if (!triggerOn || isPause == true) return;       

        //Debug.Log("충돌감지");
        //Debug.Log(tile.GetComponent<Tilemap>().color);

        if (tile.GetComponent<Tilemap>().color != Color.white)
        {
            isPause = true;
            audioSource.clip = Sound_Effect[3];
            audioSource.Play();

            animator.Play("Explosion");

            Destroy(Note);

            if (Music!= null)
            {
                Music.GetComponent<Intro_Music>().backmusic.Stop();
                Music.GetComponent<Intro_Music>().Is_Pause = false;
            }

            Invoke(nameof(Set_Retry_UI), 2.0f);
                       

            /*
            Color color = sprite.color;
            color.a = 0f;
            sprite.color = color;
            */

            //Destroy(GameObject.Find("BackGroundMusic"));
            //Destroy(this);
        }
    }
    public void Resume()
    {
        Time.timeScale = 1;
        isPause = false;
        Music.GetComponent<Intro_Music>().backmusic.Play();
        Music.GetComponent<Intro_Music>().Is_Pause = false;
        GameObject.Find("Panel").SetActive(false);
                
    }

    public void Retry()
    {
        Time.timeScale = 1;
        if (Music != null)
        {
            Destroy(Music);            
        }
        
        SceneManager.LoadScene(4);

    }
    public void Title()
    {
        if (Music != null)
        {
            Destroy(Music);
            
        }
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
        
    }

    public void Exit()
    {
        Application.Quit();
    }
    public void Change_Scene(int num)
    {
        SceneManager.LoadScene(num);
    }

}
