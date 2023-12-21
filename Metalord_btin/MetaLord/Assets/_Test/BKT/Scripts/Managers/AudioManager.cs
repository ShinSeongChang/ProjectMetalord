using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 매니저
/// 231215 배경택
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("오디오 소스")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;
    // TODO 3D로 소리를 내야 할 경우의 오디오 소스 추가

    [Header("오디오 클립"),Tooltip("필요한 오디오 클립을 끌어다 넣어주세요")]
    public AudioClip clip_Background;
    public AudioClip clip_Getcoin;
    public AudioClip clip_Jump;
    //TODO 필요한 음악 클립 추가

    private void Start()
    {
        PlayBackGroundMusic(); // 최초 음악시작 -> 음악시작위치 조정 필요시 위치 변경
    }

    // 최초 음악 시작 시점
    public void PlayBackGroundMusic()
    {
        if(clip_Background != null)
        {
            musicSource.clip = clip_Background;
            musicSource.Play();
        }
        else Debug.Log("오디오 파일이 없습니다.");
    }

    // 배경 음악 변경시
    public void ChangeBackGroundMusic(AudioClip clip)
    {
        if(clip != null) musicSource.clip = clip;
        else Debug.Log("오디오 파일이 없습니다.");
    }

    // 효과음 실행시
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) SFXSource.PlayOneShot(clip);
        else Debug.Log("오디오 파일이 없습니다.");
    }
}