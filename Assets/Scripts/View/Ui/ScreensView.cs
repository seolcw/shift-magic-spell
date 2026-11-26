using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 화면 위에 뜨는 패널(Screens 오브젝트 아래): 시작, 일시정지, 쓰러짐, 페이즈 클리어, 다음 페이즈 배너, FINAL 배너, 엔딩 자막, 승리, 번쩍임.
    /// 패널마다 자식 오브젝트(덮개, 글자)를 에디터에서 고친다. 상태에 따라 하나만 켠다. 문구의 {0} 자리는 코드가 채운다.
    /// </summary>
    public class ScreensView : MonoBehaviour
    {
        [SerializeField] private GameObject _title;
        [Tooltip("첫 화면 키캡 로고 연출(제목 줄이 밀려 맞춰짐, CLICK TO START 포함)")]
        [SerializeField] private TitleLogoView _titleLogo;
        [Tooltip("첫 화면 오른쪽 아래 버전 글자")]
        [SerializeField] private PixelText _version;
        [SerializeField] private GameObject _paused;
        [SerializeField] private GameObject _dead;
        [Tooltip("{0} = 페이즈, {1} = 재시작할 손 HP, {2} = 최대")]
        [SerializeField] private PixelText _deadSubtitle;
        [Tooltip("{0} = 마지막으로 맞은 공격")]
        [SerializeField] private PixelText _deadCause;
        [SerializeField] private GameObject _phaseClear;
        [Tooltip("{0} = 클리어한 페이즈")]
        [SerializeField] private PixelText _phaseClearTitle;
        [Tooltip("전환 시간 중 이 비율부터 클리어 글자를 띄운다(앞부분은 마법이 손에 맞는 장면)")]
        [SerializeField, Range(0f, 1f)] private float _phaseClearShowFrom = 0.5f;
        [Tooltip("다음 페이즈 배너(판 + 큰 글자)")]
        [SerializeField] private GameObject _phaseIntro;
        [Tooltip("{0} = 시작하는 페이즈")]
        [SerializeField] private PixelText _phaseIntroTitle;
        [Tooltip("마지막 페이즈에서 SHIFT로 바뀔 때 배너")]
        [SerializeField] private GameObject _final;

        [Header("번쩍임 (배너가 뜨는 순간)")]
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private float _flashTime = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.6f;

        [Header("엔딩 자막 (문구·시각은 AttackPatterns.Ending)")]
        [SerializeField] private GameObject _ending;
        [SerializeField] private PixelText _endingCaption;

        [Header("승리")]
        [SerializeField] private GameObject _win;
        [Tooltip("{0} = 분, {1} = 초, {2} = 사망, {3} = 피격")]
        [SerializeField] private PixelText _winLine1;
        [Tooltip("{0} = 완성 단어, {1} = 줄 돌리기, {2} = 콤보")]
        [SerializeField] private PixelText _winLine2;
        [Tooltip("엔딩 연출이 끝난 뒤 결과 화면을 띄우기까지(초)")]
        [SerializeField] private float _winShowAfterEnding = 0.2f;

        private void Start()
        {
            if (_version != null)
            {
                _version.SetDisplay(Application.version);
            }
        }

        public void Render(GameSession game)
        {
            float clearTime = game.Config.PhaseClearTime;
            bool isPhaseClear = game.State == GameState.PhaseClear && game.StateT < clearTime * (1f - _phaseClearShowFrom);
            bool isPhaseIntro = game.State == GameState.PhaseIntro;
            bool isFinal = game.State == GameState.Play && game.FinisherIntroT > 0f;
            bool isWin = game.State == GameState.Win && game.StateT >= game.Data.Ending.EndAt + _winShowAfterEnding;
            bool isEndingCaption = game.State == GameState.Win && !isWin && game.EndingCaptionLength > 0;
            SetActive(_title, game.State == GameState.Ready);
            if (game.State == GameState.Ready && _titleLogo != null && _titleLogo.isActiveAndEnabled)
            {
                _titleLogo.Render(Time.deltaTime);
            }

            SetActive(_paused, game.IsPaused && game.State != GameState.Ready);
            SetActive(_dead, game.State == GameState.Dead);
            SetActive(_phaseClear, isPhaseClear);
            SetActive(_phaseIntro, isPhaseIntro);
            SetActive(_final, isFinal);
            RenderFlash(game, isPhaseIntro, isFinal);
            if (isPhaseIntro)
            {
                _phaseIntroTitle.SetArgs(game.Phase + 1);
            }

            SetActive(_ending, isEndingCaption);
            if (isEndingCaption && _endingCaption != null)
            {
                _endingCaption.SetDisplay(game.Data.Ending.Caption.Substring(0, game.EndingCaptionLength));
            }

            SetActive(_win, isWin);

            if (game.State == GameState.Dead)
            {
                _deadSubtitle.SetArgs(game.Phase + 1, game.GetRecoveredHandHp(), game.Hand.MaxHp);
                if (_deadCause != null)
                {
                    _deadCause.SetArgs(string.IsNullOrEmpty(game.LastHitSource) ? "THE HAND" : game.LastHitSource);
                }
            }

            if (isPhaseClear)
            {
                _phaseClearTitle.SetArgs(game.Phase + 1);
            }

            if (isWin)
            {
                GameStats stats = game.Stats;
                int hits = 0;
                foreach (int count in stats.Hits.Values)
                {
                    hits += count;
                }

                int seconds = Mathf.FloorToInt(stats.PlayTime);
                _winLine1.SetArgs(seconds / 60, seconds % 60, stats.Deaths, hits);
                _winLine2.SetArgs(stats.WordsDone, stats.Rotations, stats.Combos);
            }
        }

        // 배너가 뜨는 순간 화면 전체가 하얗게 번쩍였다가 사라진다
        private void RenderFlash(GameSession game, bool isPhaseIntro, bool isFinal)
        {
            if (_flash == null)
            {
                return;
            }

            float elapsed = float.MaxValue;
            if (isPhaseIntro)
            {
                elapsed = game.Config.PhaseIntroTime - game.StateT;
            }
            else if (isFinal)
            {
                elapsed = game.Config.FinisherIntroTime - game.FinisherIntroT;
            }

            float alpha = elapsed < _flashTime ? _flashAlpha * (1f - elapsed / _flashTime) : 0f;
            _flash.enabled = alpha > 0f;
            _flash.color = new Color(1f, 1f, 1f, alpha);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
