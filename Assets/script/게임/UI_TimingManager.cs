using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class UI_TimingManager : MonoBehaviour
{
    public List<GameObject> boxNoteList = new List<GameObject>();

    [SerializeField] Transform Center = null;
    [SerializeField] RectTransform[] timingRect = null;
    Vector2[] timingBoxes = null;

    public Text Percent;
    public GameObject DALT;
    float DALT_x;

    public Text Score_Text;
    public Text Score_Text2;
    int Score;
    
        private void Start()
    {
        DALT_x = 0;
        Score = 0;

        timingBoxes = new Vector2[timingRect.Length];

        for(int i=0; i<timingRect.Length; i++)
        {
            timingBoxes[i].Set(Center.localPosition.x - timingRect[i].rect.width / 2,
                Center.localPosition.x + timingRect[i].rect.width / 2);
        }
    }

    private void Update()
    {
        Percent.text = (DALT_x*100/257f).ToString("0.0") + " %";
        Score_Text.text = Score.ToString();
        Score_Text2.text = Score.ToString();
    }
    public void CheckTiming()
    {
        for (int i=0; i < boxNoteList.Count; i++)
        {
            float t_notePosX = boxNoteList[i].transform.localPosition.x;
            for(int x=0; x<timingBoxes.Length; x++)
            {
                if(timingBoxes[x].x <= t_notePosX && t_notePosX <= timingBoxes[x].y)
                {
                    Destroy(boxNoteList[i]);
                    boxNoteList.RemoveAt(i);
                    
                    Debug.Log("Hit" + x);

                    Score += 100;
                    DALT_x += 1;
                    return;
                }
            }
        }

        Debug.Log("Miss");
        GameObject.Find("DALT").GetComponent<Game_Button>().currentHp -=1;
        DALT_x += 1;
        Score -= 50;

    }
}
