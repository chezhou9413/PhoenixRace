using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Tool
{
   public static class OrangeMaterialCache
    {
        private static Dictionary<Material, Material> _cache = new Dictionary<Material, Material>();

        // 橙色定义
        private static readonly Color OrangeEmission = new Color(1f, 0.5f, 0f, 1f);

        public static Material GetOrangeVersion(Material original, Pawn pawn)
        {
            if (original == null) return null;

            if (_cache.TryGetValue(original, out Material cachedMat))
            {
                return cachedMat;
            }

            Material newMat = new Material(ShaderDatabase.TransparentPostLight);
            newMat.mainTexture = original.mainTexture;

            if (original.HasProperty(ShaderPropertyIDs.MaskTex))
            {
                newMat.SetTexture(ShaderPropertyIDs.MaskTex, original.GetTexture(ShaderPropertyIDs.MaskTex));
            }

            newMat.color = OrangeEmission;
            _cache[original] = newMat;

            return newMat;
        }
    }
}
