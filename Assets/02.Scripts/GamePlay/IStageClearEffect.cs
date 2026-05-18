using System.Collections;

public interface IStageClearEffect
{
    // 순차적 실행을 위한 코루틴 함수
    IEnumerator Execute();
    
    // 스테이지 재시작 시 상태를 되돌리기 위한 함수
    void ResetEffect();
}