-- 엔딩 연출 에셋: 거대한 손이 고장 난 커서였다(금이 가고 → 모래시계 → 텍스트 커서 I → 작은 화살표). 기존 에셋은 건드리지 않는다.
-- 실행:
--   Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_assets_ending.lua
-- 규칙은 build_assets.lua와 같다: fss_lib의 12색 팔레트, 검은 1px 외곽선, 빛은 왼쪽 위.
-- CursorShift는 Hand와 같은 32x32 칸이라 HandView가 손 그림 대신 그대로 바꿔 끼운다.

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"

-- 같은 이름이 이어지는 프레임을 한 태그로 묶어 저장한다. (build_assets.lua와 같은 방식)
local function saveTagged(path, w, h, list)
  local frames, tags = {}, {}
  for i, f in ipairs(list) do frames[i] = { draw = f.draw, ms = f.ms or 100 } end
  local i = 1
  while i <= #list do
    local j = i
    while list[j + 1] and list[j + 1].name == list[i].name do j = j + 1 end
    tags[#tags + 1] = { list[i].name, i, j }
    i = j + 1
  end
  L.buildSprite(path, w, h, frames, tags)
end

local function maskOf(w, h, f)
  local rows = {}
  for y = 0, h - 1 do
    local t = {}
    for x = 0, w - 1 do t[#t + 1] = f(x, y) or "." end
    rows[#rows + 1] = table.concat(t)
  end
  return rows
end

----------------------------------------------------------------------
-- 금 간 손: Hand의 Idle 첫 장을 그대로 복사하고, 몸통(흰·회색) 위에만 검은 금을 긋는다
----------------------------------------------------------------------
local handIdle = L.load(art .. "Hand/Hand.aseprite")
local idleFrame = handIdle.frames[handIdle.tags["Idle"][1]]

-- 금: 손바닥 오른쪽 위에서 왼쪽 아래로, 두 번째 장은 갈래가 더 뻗는다
local CRACK_1 = { { 22, 15 }, { 21, 16 }, { 21, 17 }, { 20, 18 }, { 19, 19 }, { 19, 20 }, { 18, 21 }, { 17, 22 } }
local CRACK_2 = {
  { 22, 15 }, { 21, 16 }, { 21, 17 }, { 20, 18 }, { 19, 19 }, { 19, 20 }, { 18, 21 }, { 17, 22 }, { 16, 23 }, { 15, 23 }, { 14, 24 }, { 13, 25 },
  { 20, 19 }, { 21, 20 }, { 22, 20 }, { 23, 21 }, { 24, 22 },
  { 17, 21 }, { 16, 20 }, { 15, 20 }, { 14, 19 },
  { 12, 8 }, { 12, 9 }, { 11, 10 }, { 12, 11 }, { 12, 12 },
}

local function crackedHand(points)
  return function(img)
    L.blit(img, idleFrame, 0, 0)
    for _, p in ipairs(points) do
      local c = img:getPixel(p[1], p[2])
      if app.pixelColor.rgbaA(c) > 0 then L.px(img, p[1], p[2], "K") end
    end
  end
end

----------------------------------------------------------------------
-- 모래시계(로딩 커서): 위아래 받침 + 유리. 두 장은 모래가 위 → 아래로 내려간 모습(돌리는 건 코드가)
----------------------------------------------------------------------
local GLASS = {
  "##############",
  "##############",
  ".############.",
  ".############.",
  "..##########..",
  "...########...",
  "....######....",
  ".....####.....",
  "......##......",
  ".....####.....",
  "....######....",
  "...########...",
  "..##########..",
  ".############.",
  ".############.",
  "##############",
  "##############",
}

local function hourglass(sandTop)
  return function(img)
    local ox, oy = 9, 7
    L.drawMask(img, GLASS, ox, oy, "W", "L")
    -- 받침은 회색
    L.rect(img, ox, oy, ox + 13, oy + 1, "M")
    L.rect(img, ox, oy + 15, ox + 13, oy + 16, "M")
    -- 모래
    if sandTop then
      L.drawMap(img, { "YYYYYYYYYY", ".YYYYYYYY.", "..YYYYYY.." }, ox + 2, oy + 3)
      L.px(img, ox + 6, oy + 8, "Y"); L.px(img, ox + 7, oy + 8, "Y")
      L.drawMap(img, { "....YY....", ".YYYYYYYY." }, ox + 2, oy + 12)
    else
      L.drawMap(img, { "....YY...." }, ox + 2, oy + 4)
      L.px(img, ox + 6, oy + 8, "Y"); L.px(img, ox + 7, oy + 8, "Y")
      L.drawMap(img, { "...YYYY...", ".YYYYYYYY.", "YYYYYYYYYY" }, ox + 2, oy + 11)
    end
  end
end

----------------------------------------------------------------------
-- 텍스트 커서 I(I-beam)
----------------------------------------------------------------------
local BEAM = { "###.###" }
for _ = 1, 16 do BEAM[#BEAM + 1] = "...#..." end
BEAM[#BEAM + 1] = "###.###"

----------------------------------------------------------------------
-- 작은 화살표 커서(평범한 마우스 포인터)
----------------------------------------------------------------------
local ARROW = {
  "#",
  "##",
  "###",
  "####",
  "#####",
  "######",
  "#######",
  "########",
  "#########",
  "#####",
  "##.##",
  "#...##",
  ".....##",
  "......#",
}

saveTagged(art .. "Hand/CursorShift.aseprite", 32, 32, {
  { name = "Crack", ms = 200, draw = crackedHand(CRACK_1) },
  { name = "Crack", ms = 200, draw = crackedHand(CRACK_2) },
  { name = "Hourglass", ms = 250, draw = hourglass(true) },
  { name = "Hourglass", ms = 250, draw = hourglass(false) },
  { name = "Beam", draw = function(img) L.drawMask(img, BEAM, 13, 6, "W", "W") end },
  { name = "Arrow", draw = function(img) L.drawMask(img, ARROW, 12, 9, "W", "L") end },
})

----------------------------------------------------------------------
-- 펑 연기 16x16: 커서 모양이 바뀔 때. 작은 구름 → 크게 퍼짐 → 흩어짐
----------------------------------------------------------------------
local function dist(x, y, cx, cy) return math.sqrt((x - cx) ^ 2 + (y - cy) ^ 2) end
local function puffs(list)
  return maskOf(16, 16, function(x, y)
    for _, c in ipairs(list) do if dist(x, y, c[1], c[2]) <= c[3] then return "#" end end
  end)
end

saveTagged(art .. "Fx/Poof.aseprite", 16, 16, {
  { name = "Poof", ms = 60, draw = function(img) L.drawMask(img, puffs({ { 7.5, 7.5, 3.2 }, { 5, 9, 2 }, { 10, 9, 2 } }), 0, 0, "W", "L") end },
  { name = "Poof", ms = 80, draw = function(img) L.drawMask(img, puffs({ { 4, 5, 2.6 }, { 11, 4, 2.4 }, { 3, 11, 2.4 }, { 12, 11, 2.8 }, { 7.5, 8, 2.6 } }), 0, 0, "W", "L") end },
  { name = "Poof", ms = 100, draw = function(img) L.drawMask(img, puffs({ { 2.5, 3, 1.5 }, { 13, 2.5, 1.5 }, { 2, 13, 1.4 }, { 13.5, 13, 1.6 } }), 0, 0, "L", "M") end },
})

print("FSS ending build done")
