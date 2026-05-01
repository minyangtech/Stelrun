using UnityEngine;

public class Quit : MonoBehaviour
{
    public void Quita()
    {
        // 실제 빌드된 게임 종료
        Application.Quit();

        // 유니티 에디터의 Play 모드도 종료 (테스트용)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}