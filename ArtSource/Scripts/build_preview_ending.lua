-- 엔딩 연출 에셋 확인 그림: ArtSource/Preview/asset-sheet-ending.png (3배)
-- 실행: Aseprite.exe -b --script-param root=<저장소 경로> --script ArtSource/Scripts/build_preview_ending.lua

local root = app.params.root
local L = dofile(root .. "/ArtSource/Scripts/fss_lib.lua")
local art = root .. "/Assets/Art/"

local W, H = 260, 110
local img = Image(W, H, ColorMode.RGB)
L.rect(img, 0, 0, W - 1, H - 1, "K")
local x, y = 6, 6

L.drawText(img, "ENDING", 6, y, "W")
y = y + 12

-- 손 Idle → 금 간 손 2장 → 모래시계 2장 → I → 화살표 (연출 순서대로)
local hand = L.load(art .. "Hand/Hand.aseprite")
local shift = L.load(art .. "Hand/CursorShift.aseprite")
L.blit(img, hand.frames[hand.tags["Idle"][1]], x, y)
x = x + 36
for _, name in ipairs(shift.order) do
  local t = shift.tags[name]
  for i = t[1], t[2] do
    L.blit(img, shift.frames[i], x, y)
    x = x + 34
  end
end
y = y + 36
x = 6
L.drawText(img, "IDLE CRACK HOURGLASS BEAM ARROW", x, y, "M")
y = y + 14

local poof = L.load(art .. "Fx/Poof.aseprite")
for i = 1, #poof.frames do
  L.blit(img, poof.frames[i], x, y)
  x = x + 20
end
L.drawText(img, "POOF", x + 4, y + 5, "M")

L.savePngScaled(img, root .. "/ArtSource/Preview/asset-sheet-ending.png", 3)
print("FSS ending preview done")
