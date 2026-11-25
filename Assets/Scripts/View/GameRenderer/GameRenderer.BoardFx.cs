using System.Collections.Generic;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    public partial class GameRenderer
    {
        /// <summary>
        /// 플레이어 줄 돌리기: 줄 전체에 파란 띠가 깔리고, 그 위로 › 가 돌리는 방향으로 흐른다.
        /// 양 끝 바깥에는 큰 화살표: 한쪽 끝에서 밀려 나간 키가 반대쪽 끝으로 들어온다는 뜻.
        /// </summary>
        private void DrawRotateFx(FxEvent fx)
        {
            float u = fx.T / fx.Duration;
            List<BoardCell> cells = _game.Boards[fx.Board].LineCells(fx.Axis, fx.Index);
            cells.RemoveAll(cell => cell.IsShift);
            if (cells.Count == 0)
            {
                return;
            }

            bool isRow = fx.Axis == LineAxis.Row;
            var unit = new Vector2(isRow ? fx.Sign : 0f, isRow ? 0f : fx.Sign);
            float angle = -Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg;
            float flow = ((1f - u) * 2.5f) % 1f;
            foreach (BoardCell cell in cells)
            {
                Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.R, cell.C);
                _canvas.DrawRect(topLeft.x, topLeft.y, ViewUtils.CellW, ViewUtils.CellW, Colors.Shift.WithAlpha(u * 0.3f));
            }

            Color arrowTint = Color.white.WithAlpha(u);
            foreach (BoardCell cell in cells)
            {
                // 글자를 가리지 않게 키 가장자리(행은 아래, 열은 오른쪽)에
                Vector2 center = ViewUtils.CellCenter(fx.Board, cell.R, cell.C);
                Vector2 edge = isRow ? new Vector2(0f, 6f) : new Vector2(6f, 0f);
                Vector2 travel = ViewUtils.ToPixelDelta(unit * (flow - 0.5f));
                Vector2 position = center + edge + travel;
                _canvas.DrawFrameCentered(Frame("RotateChevron", "Chevron"), position.x, position.y, arrowTint, angle);
            }

            Vector2 first = ViewUtils.CellCenter(fx.Board, cells[0].R, cells[0].C);
            Vector2 last = ViewUtils.CellCenter(fx.Board, cells[cells.Count - 1].R, cells[cells.Count - 1].C);
            Vector2 outEnd = fx.Sign > 0 ? last : first;
            Vector2 inEnd = fx.Sign > 0 ? first : last;
            Vector2 gap = ViewUtils.ToPixelDelta(unit * 0.85f);
            _canvas.DrawFrameCentered(Frame("EdgeArrow", "Arrow"), outEnd.x + gap.x, outEnd.y + gap.y, arrowTint, angle);
            _canvas.DrawFrameCentered(Frame("EdgeArrow", "Arrow"), inEnd.x - gap.x, inEnd.y - gap.y, arrowTint, angle);
        }

        // 칸에 붙는 짧은 연출. 판 위 연출은 over 캔버스, 마법·글자·클릭 고리는 fx 캔버스에 그린다
        private void DrawCellFx()
        {
            foreach (FxEvent fx in _game.Fx)
            {
                UseCanvas(IsFxLayer(fx.Kind) ? _fxCanvas : _overCanvas);
                switch (fx.Kind)
                {
                    case FxKind.Rotate:
                        DrawRotateFx(fx);
                        break;
                    case FxKind.Spell:
                        DrawSpell(fx);
                        break;
                    case FxKind.Debris:
                        DrawDebris(fx);
                        break;
                    case FxKind.Match:
                        {
                            // 맞춘 칸이 초록으로 번쩍이며 퍼진다
                            float u = fx.T / fx.Duration;
                            float grow = (1f - u) * 3f;
                            foreach (Vector2Int cell in fx.Cells)
                            {
                                Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.y, cell.x);
                                _canvas.DrawRect(topLeft.x - grow, topLeft.y - grow, ViewUtils.CellW + grow * 2f, ViewUtils.CellW + grow * 2f, Colors.Ok.WithAlpha(u * 0.8f));
                            }

                            break;
                        }

                    case FxKind.Store:
                        {
                            // 완성한 단어가 블록이 되어 반대편 슬롯으로 포물선을 그리며 날아간다
                            float u = 1f - fx.T / fx.Duration;
                            float eased = 1f - (1f - u) * (1f - u);
                            Vector2 from = ToPixel(fx.From);
                            Vector2 to = ToPixel(fx.To);
                            Vector2 position = Vector2.Lerp(from, to, eased) - new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 28f);
                            SpellDef spell = _game.Data.GetSpellOrNull(fx.Word);
                            _canvas.DrawFrameCentered(Frame("AttackBlock", "Idle"), position.x, position.y, fx.Color);
                            _canvas.DrawFrameCentered(Frame("SpellIcon", spell?.Icon), position.x, position.y - 2f, Color.white);
                            break;
                        }

                    case FxKind.Fizzle:
                        {
                            // 블록이 깨지거나 사라질 때 조각이 흩어진다
                            Vector2 center = ViewUtils.CellCenter(fx.Board, fx.R, fx.C);
                            float u = 1f - fx.T / fx.Duration;
                            for (int i = 0; i < 6; i++)
                            {
                                float angle = i * Mathf.PI / 3f;
                                Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (14f * u);
                                _canvas.DrawFrameCentered(Animated("BlockDebris", "Debris", u), position.x, position.y + u * u * 8f, Color.white.WithAlpha(1f - u));
                            }

                            break;
                        }

                    case FxKind.Click:
                        {
                            Vector2 center = ToPixel(fx.From);
                            _canvas.DrawRing(center, 3f + (fx.Duration - fx.T) * 40f, 1, Colors.Warn.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Impact:
                        foreach (Vector2Int cell in fx.Cells)
                        {
                            Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, cell.y, cell.x);
                            _canvas.DrawRect(topLeft.x + 1f, topLeft.y + 1f, 14f, 14f, Colors.Warn.WithAlpha(fx.T / fx.Duration * 0.6f));
                        }

                        break;
                    case FxKind.Warp:
                        {
                            Vector2 center = ViewUtils.CellCenter(fx.Board, fx.R, fx.C);
                            float u = 1f - fx.T / fx.Duration;
                            _canvas.DrawRing(center, 4f + u * 28f, 1, Colors.Shift.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Deny:
                        {
                            Vector2 topLeft = ViewUtils.CellTopLeft(fx.Board, fx.R, fx.C);
                            _canvas.DrawRectOutline(topLeft.x - 1f, topLeft.y - 1f, 18f, 18f, 1f, Colors.Dim.WithAlpha(fx.T / fx.Duration));
                            break;
                        }

                    case FxKind.Float:
                        {
                            if (fx.Age < fx.Delay)
                            {
                                break;
                            }

                            Vector2 position = ToPixel(fx.From) - new Vector2(0f, Mathf.Clamp01(1f - fx.T / fx.Duration) * 10f);
                            PixelText text = _floatTextPool.Next();
                            text.transform.localPosition = ViewUtils.PixelToWorld(position);
                            text.SetDisplay(fx.Text);
                            text.Color = fx.Color;
                            text.Alpha = Mathf.Min(1f, fx.T * 2f);
                            break;
                        }

                    default:
                        break;
                }
            }
        }

        // 블록이 든 슬롯을 가리키는 화살표: 판 바깥에서 슬롯 쪽으로 까딱인다(플레이어가 그 슬롯 위에 있으면 숨김)
        private void DrawSlotArrows()
        {
            if (_game.State != GameState.Play || HudView.GetPendingBlockBoard(_game) < 0)
            {
                return;
            }

            float bob = Mathf.Round(Mathf.Sin(_game.Time * 8f) * 2f);
            foreach (BoardModel board in _game.Boards)
            {
                foreach (BoardCell slot in board.Slots)
                {
                    if (slot.Block == null)
                    {
                        continue;
                    }

                    Vector2 center = ViewUtils.CellCenter(board.Index, slot.R, slot.C);
                    bool isLeft = board.Index == 0;
                    float x = isLeft ? center.x - ViewUtils.CellW * 0.5f - 7f - bob : center.x + ViewUtils.CellW * 0.5f + 7f + bob;
                    _canvas.DrawFrameCentered(Frame("EdgeArrow", "Arrow"), x, center.y, Color.white, isLeft ? 0f : 180f);
                }
            }
        }

        private static bool IsFxLayer(FxKind kind)
        {
            return kind == FxKind.Spell || kind == FxKind.Float || kind == FxKind.Click || kind == FxKind.Debris;
        }

        /// <summary>
        /// 타격감 조각: 맞은 자리에서 밀린 방향 쪽으로 부채꼴로 튀었다가 떨어진다. 조각마다 각도·속도는 Seed로 정해져 매 프레임 같다.
        /// player = 분홍 점, hand = 흰 점과 마법 색 별, block = 블록 조각(마법 색).
        /// </summary>
        private void DrawDebris(FxEvent fx)
        {
            float age = fx.Duration - fx.T;
            float u = Mathf.Clamp01(age / fx.Duration);
            Color fade = Color.white.WithAlpha(1f - u * u);
            Vector2 origin = ToPixel(fx.From);
            Vector2 dir = fx.To.sqrMagnitude > 0.0001f ? fx.To.normalized : Vector2.up;
            float baseAngle = Mathf.Atan2(dir.y, dir.x);
            for (int i = 0; i < fx.Count; i++)
            {
                float angle = baseAngle + (Hash(fx.Seed, i, 1) - 0.5f) * _debrisSpread;
                float speed = Mathf.Lerp(_debrisSpeed.x, _debrisSpeed.y, Hash(fx.Seed, i, 2));
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Vector2 position = origin + velocity * age + new Vector2(0f, 0.5f * _debrisGravity * age * age);
                float x = Mathf.Round(position.x);
                float y = Mathf.Round(position.y);
                switch (fx.Style)
                {
                    case "hand":
                        if (i % 2 == 0)
                        {
                            _canvas.DrawRect(x - 1f, y - 1f, 2f, 2f, Color.white * fade);
                        }
                        else
                        {
                            _canvas.DrawFrameCentered(Animated("Star", "Twinkle", age), x, y, fx.Color * fade);
                        }

                        break;
                    case "block":
                        _canvas.DrawFrameCentered(Animated("BlockDebris", "Debris", age), x, y, fx.Color * fade);
                        break;
                    default:
                        float size = i % 3 == 0 ? 2f : 1f;
                        _canvas.DrawRect(x, y, size, size, fx.Color * fade);
                        break;
                }
            }
        }

        // 0~1 결정적 난수(같은 조각은 매 프레임 같은 값)
        private static float Hash(float seed, int index, int salt)
        {
            float value = Mathf.Sin(seed * 12.9898f + index * 78.233f + salt * 37.719f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }
    }
}
