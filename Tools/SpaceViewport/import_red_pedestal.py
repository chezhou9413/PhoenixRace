import argparse
import shutil
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[2]

#将项目内维护的红台座美术及编辑器构建脚本同步到 Unity 工程。
def main():
    parser = argparse.ArgumentParser(description='同步舷窗美术和资源包构建脚本')
    parser.add_argument('--unity-project', default='E:/mygame/PhoenixRace')
    unity = Path(parser.parse_args().unity_project)
    artwork = PROJECT / 'ArtSource/SpaceViewport/RedPedestalUI'
    shutil.copytree(artwork, unity / 'Assets/Art/UI/RedPedestalUI', dirs_exist_ok=True)
    scripts = Path(__file__).resolve().parent / 'Editor'
    shutil.copytree(scripts, unity / 'Assets/Editor/SpaceViewport', dirs_exist_ok=True)
    shutil.copytree(PROJECT / 'ArtSource/SpaceViewport/Hologram',
                    unity / 'Assets/Art/UI/PhoenixHologram', dirs_exist_ok=True)
    shutil.copytree(PROJECT / 'ArtSource/SpaceViewport/ShipModel',
                    unity / 'Assets/Art/Models', dirs_exist_ok=True)
    print(f'已同步红台座预制体、图片和构建脚本到 {unity}')

if __name__ == '__main__':
    main()
