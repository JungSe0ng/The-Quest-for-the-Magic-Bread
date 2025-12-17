using UnityEngine;
using UnityEngine.Playables;

public class PlayableAssetSwitcher : MonoBehaviour
{
    [Header("Timeline Director")]
    [SerializeField] private PlayableDirector director;

    [Header("Playable Assets")]
    [SerializeField] private PlayableAsset[] playableAssets; // 여러 타임라인 에셋들
    [SerializeField] private int currentAssetIndex = 0;

    [Header("Settings")]
    [SerializeField] private bool autoPlayNext = true; // 자동으로 다음 재생
    [SerializeField] private float delayBetweenAssets = 0f; // 전환 사이 대기 시간

    private bool isTransitioning = false;

    void Start()
    {
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }

        if (director != null && autoPlayNext)
        {
            // 타임라인 종료 이벤트 등록
            director.stopped += OnPlayableFinished;
        }

        // 첫 번째 타임라인 재생
        if (playableAssets.Length > 0)
        {
            PlayAsset(0);
        }
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnPlayableFinished;
        }
    }

    // 현재 Playable이 끝났을 때
    private void OnPlayableFinished(PlayableDirector finishedDirector)
    {
        if (isTransitioning) return;

        if (autoPlayNext)
        {
            PlayNextAsset();
        }
    }

    // 다음 Playable Asset 재생
    public void PlayNextAsset()
    {
        int nextIndex = currentAssetIndex + 1;
        
        if (nextIndex < playableAssets.Length)
        {
            if (delayBetweenAssets > 0)
            {
                isTransitioning = true;
                Invoke(nameof(PlayNextDelayed), delayBetweenAssets);
            }
            else
            {
                PlayAsset(nextIndex);
            }
        }
        else
        {
            Debug.Log("모든 Playable Asset 재생 완료!");
        }
    }

    private void PlayNextDelayed()
    {
        isTransitioning = false;
        PlayAsset(currentAssetIndex + 1);
    }

    // 특정 인덱스의 Playable Asset 재생
    public void PlayAsset(int index)
    {
        if (index < 0 || index >= playableAssets.Length)
        {
            Debug.LogError($"잘못된 인덱스: {index}");
            return;
        }

        if (playableAssets[index] == null)
        {
            Debug.LogError($"Playable Asset이 null입니다. Index: {index}");
            return;
        }

        currentAssetIndex = index;
        director.playableAsset = playableAssets[index];
        director.time = 0; // 처음부터 재생
        director.Play();

        Debug.Log($"Playable Asset {index} 재생 시작: {playableAssets[index].name}");
    }

    // 이전 Playable Asset 재생
    public void PlayPreviousAsset()
    {
        int prevIndex = currentAssetIndex - 1;
        if (prevIndex >= 0)
        {
            PlayAsset(prevIndex);
        }
    }

    // 현재 재생 중인 Playable 정지
    public void Stop()
    {
        if (director != null)
        {
            director.Stop();
        }
    }

    // 현재 재생 중인 Playable 일시정지
    public void Pause()
    {
        if (director != null)
        {
            director.Pause();
        }
    }

    // 재개
    public void Resume()
    {
        if (director != null)
        {
            director.Resume();
        }
    }

    // 특정 이름의 Playable Asset 재생
    public void PlayAssetByName(string assetName)
    {
        for (int i = 0; i < playableAssets.Length; i++)
        {
            if (playableAssets[i] != null && playableAssets[i].name == assetName)
            {
                PlayAsset(i);
                return;
            }
        }
        Debug.LogError($"'{assetName}' 이름의 Playable Asset을 찾을 수 없습니다.");
    }
}
