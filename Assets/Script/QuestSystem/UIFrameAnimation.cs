 using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Memutar urutan frame Sprite pada komponen UI Image (misal animasi membuka/menutup buku).
    /// Menggunakan Time.unscaledDeltaTime agar tetap berjalan mulus meskipun game dijeda (Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIFrameAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image targetImage;

        [Header("Frame Settings")]
        [SerializeField] private Sprite[] frames;
        [SerializeField, Range(1f, 60f)] private float framesPerSecond = 24f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool playOnEnable = false;

        private Coroutine activeAnimationCoroutine;
        private int currentFrameIndex = 0;

        public bool IsPlaying => activeAnimationCoroutine != null;
        public int CurrentFrameIndex => currentFrameIndex;
        public int TotalFrames => frames != null ? frames.Length : 0;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable && frames != null && frames.Length > 0)
            {
                PlayForward();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            SetFrame(0);
        }

        public void SetFrame(int index)
        {
            if (frames == null || frames.Length == 0 || targetImage == null) return;

            currentFrameIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            targetImage.sprite = frames[currentFrameIndex];
        }

        public void PlayForward(Action onComplete = null)
        {
            if (frames == null || frames.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Stop();
            activeAnimationCoroutine = StartCoroutine(AnimateRoutine(0, frames.Length - 1, 1, onComplete));
        }

        public void PlayReverse(Action onComplete = null)
        {
            if (frames == null || frames.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Stop();
            activeAnimationCoroutine = StartCoroutine(AnimateRoutine(frames.Length - 1, 0, -1, onComplete));
        }

        public void Stop()
        {
            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
                activeAnimationCoroutine = null;
            }
        }

        private IEnumerator AnimateRoutine(int startIndex, int endIndex, int step, Action onComplete)
        {
            currentFrameIndex = startIndex;
            float interval = 1f / Mathf.Max(1f, framesPerSecond);

            while (true)
            {
                if (targetImage != null && frames != null && currentFrameIndex >= 0 && currentFrameIndex < frames.Length)
                {
                    targetImage.sprite = frames[currentFrameIndex];
                }

                if (currentFrameIndex == endIndex)
                {
                    if (loop)
                    {
                        currentFrameIndex = startIndex;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    currentFrameIndex += step;
                }

                // Gunakan WaitForSecondsRealtime agar tidak terpengaruh Time.timeScale = 0
                yield return new WaitForSecondsRealtime(interval);
            }

            activeAnimationCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
