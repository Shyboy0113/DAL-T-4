using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_NoteManager : MonoBehaviour
{
    public int bpm = 120;
    double currentTime = 0d;

    public int check;

    [SerializeField] Transform NoteAppear = null;
    [SerializeField] GameObject Note = null;

    UI_TimingManager theTimingManager;

    private void Start()
    {
        theTimingManager = GetComponent<UI_TimingManager>();
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        
        if(currentTime >= 60d / bpm)
        {
            GameObject t_note = Instantiate(Note, NoteAppear.position, Quaternion.identity);
            t_note.transform.SetParent(this.transform);
            theTimingManager.boxNoteList.Add(t_note);
            currentTime -= 60d / bpm;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Note"))
        {
            theTimingManager.boxNoteList.Remove(collision.gameObject);
            Destroy(collision.gameObject);
            GameObject.Find("DALT").GetComponent<Game_Button>().currentHp -= 1;


        }

    }
}
