"""按成年女性、站立姿态的 XML 尺寸与 HAR 图层离线合成凤凰和渡鸦。"""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont, ImageOps

PROJECT = Path(__file__).resolve().parents[1]
MOD = PROJECT / 'PhoenixRace'
RAVEN = Path('E:/RimModDev/RavenRace/ZuoYao_RavenRace')
OUT = Path('E:/ModdevMics/Outputs/PhoenixRace-1.7/Offline')
MANIFEST = json.loads((PROJECT / 'ArtSource/FH2/export_manifest.json').read_text(encoding='utf-8'))
DIRECTIONS = ('south', 'east', 'west', 'north')
FONT = 'C:/Windows/Fonts/msyh.ttc'


def vector(text):
    """读取 XML 中的二维或三维向量。"""
    return tuple(float(x) for x in text.strip('()').split(','))


def root_offset(mod, relative):
    """保留实际地图原点，不按人物边界重新居中。"""
    tree = ET.parse(mod / relative)
    return vector(tree.findtext('.//root/drawData/defaultData/offset'))[2]


def layer(mod, path, direction, width, center, depth, name):
    """把贴图记录成游戏平面中的宽度、中心和前后顺序。"""
    image = Image.open(mod / (path + '_' + direction + '.png')).convert('RGBA')
    return dict(image=image, width=width, center=center, depth=depth, name=name)


def phoenix(direction):
    """读取实际 Defs；头部像素锚点与 PhoenixHeadWorker 一致。"""
    race = ET.parse(MOD / '1.7/Defs/AlienRace/PhoenixRace.xml')
    stages = ET.parse(MOD / '1.7/Defs/AlienRace/PhoenixLifeStages.xml')
    age = stages.find('.//lifeStageAges/li[def="Phoenix_LifeStage_Adult"]')
    stage = stages.find('.//LifeStageDef[defName="Phoenix_LifeStage_Adult"]')
    size = vector(age.findtext('customDrawSize'))[0]
    body_width = float(stage.findtext('bodyWidth')) * size
    head_width = 1.5 * float(stage.findtext('headSizeFactor')) * vector(age.findtext('customHeadDrawSize'))[0]
    root = root_offset(MOD, '1.7/Defs/AlienRace/PhoenixRenderTree.xml')
    hx, hy = MANIFEST['directions'][direction]['head_center_on_body_texture_px']
    head_center = ((hx - 512) / 1024 * body_width, root + (512 - hy) / 1024 * body_width)
    body = race.findtext('.//graphicPaths/body/path')
    layers = [layer(MOD / '1.7/Textures', body, direction, body_width, (0, root), 0, 'body')]
    #原版身体网格无条件镜像西向，HAR 附件则根据独立方向贴图决定是否镜像。
    tree = ET.parse(MOD / '1.7/Defs/AlienRace/PhoenixRenderTree.xml')
    body_node = tree.findtext('.//children/li[tagDef="Body"]/nodeClass')
    if direction == 'west' and body_node == 'PawnRenderNode_Body':
        layers[0]['image'] = ImageOps.mirror(layers[0]['image'])
    for addon in race.findall('.//bodyAddons/li'):
        path = addon.findtext('path')
        head = addon.findtext('alignWithHead') == 'true'
        scale = float(addon.findtext('drawSize'))
        depth = .3 + float(addon.findtext('offsets/' + direction + '/layerOffset'))
        layers.append(layer(MOD / '1.7/Textures', path, direction,
                            scale * (head_width if head else body_width),
                            head_center if head else (0, root), depth, path.split('/')[-2]))
    return layers


def raven():
    """渡鸦正面、默认发型、无 FA；包含原版头偏移和头节点 1.21 倍变换。"""
    race = ET.parse(RAVEN / 'Defs/Core/Race/AlienRace_Raven.xml')
    stage = ET.parse(RAVEN / 'Defs/Core/Race/LifeStageDefs_Raven.xml').find('.//LifeStageDef[defName="Raven_LifeStage_Adult"]')
    age = race.find('.//lifeStageAges/li[def="Raven_LifeStage_Adult"]')
    tree = ET.parse(RAVEN / 'Defs/Core/RenderTrees/Raven_RenderTree.xml')
    head_node = vector(tree.findtext('.//children/li[tagDef="Head"]/drawSize'))[0]
    body_width = float(stage.findtext('bodyWidth')) * vector(age.findtext('customDrawSize'))[0]
    head_width = 1.5 * float(stage.findtext('headSizeFactor')) * head_node * vector(age.findtext('customHeadDrawSize'))[0]
    root = root_offset(RAVEN, 'Defs/Core/RenderTrees/Raven_RenderTree.xml')
    body_type = ET.parse('E:/steam/steamapps/common/RimWorld/Data/Core/Defs/Misc/BodyTypeDefs/BodyTypes.xml')
    base_head = vector(body_type.findtext('.//BodyTypeDef[defName="Female"]/headOffset'))[1]
    head_center = (0, root + base_head + vector(age.findtext('headFemaleOffsetDirectional/south/offset'))[1])
    texture_root = RAVEN / 'Textures'
    layers = [layer(texture_root, 'Races/Raven/Body/RavenBody_Female', 'south', body_width, (0, root), 0, 'body'),
              layer(texture_root, 'Races/Raven/Head/Blank/RavenHead_Blank', 'south', head_width, head_center, .02, 'head')]
    for addon in race.findall('.//bodyAddons/li'):
        if addon.findtext('conditions/Gender') == 'Male':
            continue
        head = addon.findtext('alignWithHead') == 'true'
        depth = float(addon.findtext('offsets/south/layerOffset', '0'))
        depth = .3 + depth if addon.findtext('inFrontOfBody') == 'true' else -.3 - depth
        path = addon.findtext('path')
        layers.append(layer(texture_root, path, 'south', float(addon.findtext('drawSize')) * (head_width if head else body_width),
                            head_center if head else (0, root), depth, addon.findtext('name', path.split('/')[-1])))
    return layers


def bounds(layers, names=None):
    """以半透明边缘的中线测量可见高度，避免零星透明像素影响结果。"""
    boxes = []
    for item in layers:
        if names and item['name'] not in names:
            continue
        image = item['image']
        box = image.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        if box:
            scale = item['width'] / image.width
            x, z = item['center']
            boxes.append((x + (box[0] - image.width / 2) * scale,
                          z + (image.height / 2 - box[3]) * scale,
                          x + (box[2] - image.width / 2) * scale,
                          z + (image.height / 2 - box[1]) * scale))
    return [min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes)]


def compose(layers, size, origin, pixels):
    """在固定世界坐标中做亚像素变换；不裁切后拉伸，也不单独移动头发。"""
    canvas = Image.new('RGBA', size)
    for item in sorted(layers, key=lambda i: i['depth']):
        source = item['image']
        scale = item['width'] * pixels / source.width
        x = origin[0] + item['center'][0] * pixels - source.width * scale / 2
        y = origin[1] - item['center'][1] * pixels - source.height * scale / 2
        transformed = source.transform(size, Image.Transform.AFFINE,
                                       (1 / scale, 0, -x / scale, 0, 1 / scale, -y / scale),
                                       Image.Resampling.BICUBIC)
        canvas.alpha_composite(transformed)
    return canvas


def label(draw, xy, text, size=24, color='#dce5ee'):
    """使用系统中文字体写入预览说明。"""
    draw.text(xy, text, fill=color, font=ImageFont.truetype(FONT, size))


def grid(canvas, origin, pixels):
    """每格一地图单位，十字标注 Pawn 世界坐标原点。"""
    d = ImageDraw.Draw(canvas)
    for i in range(-4, 5):
        x, y = origin[0] + i * pixels, origin[1] + i * pixels
        d.line((x, 130, x, canvas.height - 70), fill='#344451')
        d.line((0, y, canvas.width, y), fill='#344451')
    x, y = origin
    d.line((x - 12, y, x + 12, y), fill='#70b5b1', width=2)
    d.line((x, y - 12, x, y + 12), fill='#70b5b1', width=2)


def main():
    """输出同原点尺寸对照、四方向图和带单位的测量记录。"""
    OUT.mkdir(parents=True, exist_ok=True)
    reference = raven()
    front = phoenix('south')
    canvas = Image.new('RGBA', (1400, 900), '#22313d')
    for x in (390, 1010):
        grid(canvas, (x, 480), 400)
    for x, layers, title in [(390, reference, '渡鸦 · 成年女性 / 默认发型'), (1010, front, '凤凰 · 成年 / FH2')]:
        canvas.alpha_composite(compose(layers, canvas.size, (x, 480), 400))
        label(ImageDraw.Draw(canvas), (x - 185, 805), title)
    d = ImageDraw.Draw(canvas)
    label(d, (45, 25), '同地图原点 · 离线尺寸对照', 34)
    label(d, (45, 80), '每格 1 地图单位；双方 Root Z = +0.05；不含装备、光照、动作与运行时特效', 21)
    canvas.convert('RGB').save(OUT / 'Phoenix_Raven_size_comparison.png')
    sheet = Image.new('RGBA', (1920, 860), '#22313d')
    d = ImageDraw.Draw(sheet)
    label(d, (35, 20), '凤凰 FH2 · 四方向离线模拟', 34)
    label(d, (35, 75), '读取模组实际尺寸与图层顺序；面部为整体贴图；已移除头发投影层', 22)
    for index, direction in enumerate(DIRECTIONS):
        parts = phoenix(direction)
        origin = (240, 400)
        single = compose(parts, (480, 860), origin, 360)
        single.save(OUT / ('Phoenix_' + direction + '.png'))
        sheet.alpha_composite(single, (index * 480, 0))
        label(d, (index * 480 + 215, 785), dict(south='正面', east='右侧', west='左侧', north='背面')[direction])
    sheet.convert('RGB').save(OUT / 'Phoenix_four_directions.png')
    rb = bounds(reference, ['body', 'head', 'raven_hair', 'raven_hair_back'])
    pb = bounds(front, ['body', 'head', 'hair', 'hairback'])
    report = dict(method='成年女性站立正面，默认发型；按 alpha >= 128 测量主体（不含耳羽、翼、尾羽）',
                  raven_bounds_world=rb, phoenix_bounds_world=pb,
                  raven_height=rb[3] - rb[1], phoenix_height=pb[3] - pb[1],
                  height_ratio=(pb[3] - pb[1]) / (rb[3] - rb[1]),
                  root_z=.05, rendering='Pillow 离线平面合成，不是游戏截图')
    (OUT / 'measurements.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
