using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using TpacTool.Lib;
using TpacTool.Lib.Util;
using static OpenTK.Graphics.OpenGL4.GL;

namespace TpacTool
{
	public static class AnimationManager
	{
		private const int MAX_BONE_COUNT = 64;
        public static List<Renderer.RenderMesh> CurrentBoneMeshes { private set; get; }
        public static void CreateOctahedralBoneMeshes(Skeleton s)
        {
            if (s == null || s.Definition.Data.Bones.Count < 1)
            {
                CurrentBoneMeshes?.Clear();
                return;
            }
            CurrentBoneMeshes = new List<Renderer.RenderMesh>(s.Definition.Data.Bones.Count);
            // Ignore M44. Otherwise bone placement is messed up for human_skeleton and horse_skeleton for some reason
            var globalFrames = s.Definition.Data.CreateBoneMatrices(true);
            for (int i = 0; i < s.Definition.Data.Bones.Count; i++)
			{
				var b = s.Definition.Data.Bones[i];
                var m = globalFrames[i];
                var verticesPositions = new Vector3[6];
                var boneLength = GetBoneLength(s, i);
                for (int j = 0; j < 6; j++)
                {
                    var p = _OCTAHEDRAL_MESH_POS[j];
                    // Scale down the bone mesh
                    p *= boneLength;
                    // Apply the global transforamtion on each vertex of the bone mesh
                    p = new Vector3(m.M11 * p.X + m.M21 * p.Y + m.M31 * p.Z + m.M41,
                        m.M12 * p.X + m.M22 * p.Y + m.M32 * p.Z + m.M42,
                        m.M13 * p.X + m.M23 * p.Y + m.M33 * p.Z + m.M43);
                    verticesPositions[j] = p;
                }
                var floatPos = new float[]
                {
                    verticesPositions[0].X, verticesPositions[0].Y, verticesPositions[0].Z,
                    verticesPositions[1].X, verticesPositions[1].Y, verticesPositions[1].Z,
                    verticesPositions[2].X, verticesPositions[2].Y, verticesPositions[2].Z,
                    verticesPositions[3].X, verticesPositions[3].Y, verticesPositions[3].Z,
                    verticesPositions[4].X, verticesPositions[4].Y, verticesPositions[4].Z,
                    verticesPositions[5].X, verticesPositions[5].Y, verticesPositions[5].Z,
                };
                var mesh = new Renderer.RenderMesh();
                mesh.Mesh = new MeshManager.OglMesh(_OCTAHEDRAL_MESH_INDICES, floatPos, i);
                mesh.Texture = TextureManager.FALLBACK_TEXTURE;
                mesh.Shader = ShaderManager.MeshShader;
                CurrentBoneMeshes.Add(mesh);
            }
        }
        public static void CreateOctahedralBoneMeshes(Skeleton s, SkeletalAnimation a, float time, int start)
        {
            if (s == null || s.Definition.Data.Bones.Count < 1)
            {
                CurrentBoneMeshes?.Clear();
                return;
            }
            time += start;
            CurrentBoneMeshes = new List<Renderer.RenderMesh>(s.Definition.Data.Bones.Count);
            var matrices = new Matrix4x4[s.Definition.Data.Bones.Count];
            var parentLookUp = s.Definition.Data.CreateParentLookup();
            for (int i = 0; i < s.Definition.Data.Bones.Count; i++)
            {
                var m = Matrix4x4.Identity;
                var restMat = s.Definition.Data.Bones[i].RestFrame;
                // Ignore M44, otherwise bone placement is messed up for some skeletons
                restMat.M44 = 1f;
                Matrix4x4.Decompose(restMat, out var scale, out _, out _);
                if (i < a.Definition.Data.BoneAnims.Count)
                {
                    var q = a.Definition.Data.BoneAnims[i].GetInterpolatedRotation(time, out _, out _);
                    var t = a.Definition.Data.BoneAnims[i].GetInterpolatedPosition(time, out _, out _);
                    if (i == 0) t = a.Definition.Data.GetInterpolatedPosition(time, out _, out _);
                    // The quaternion stored in the animation seems to be the final rotation of the bone
                    // regardless of its original rest frame orientation
                    m = Matrix4x4.CreateFromQuaternion(q);
                    m.Translation = restMat.Translation + new Vector3(t.X, t.Y, t.Z);
                    // Transform matrix to global space by applying the parent's transformation first
                    if (parentLookUp[i] > -1) m = m * matrices[parentLookUp[i]];
                    matrices[i] = m;
                }
                
                var verticesPositions = new Vector3[6];
                var boneLength = GetBoneLength(s, i);
                for (int j = 0; j < 6; j++)
                {
                    var p = _OCTAHEDRAL_MESH_POS[j];
                    // Scale down the bone mesh
                    p *= boneLength;
                    // Apply the global transforamtion on each vertex of the bone mesh
                    p = new Vector3(m.M11 * p.X + m.M21 * p.Y + m.M31 * p.Z + m.M41,
                        m.M12 * p.X + m.M22 * p.Y + m.M32 * p.Z + m.M42,
                        m.M13 * p.X + m.M23 * p.Y + m.M33 * p.Z + m.M43);
                    verticesPositions[j] = p;
                }
                var floatPos = new float[]
                {
                    verticesPositions[0].X, verticesPositions[0].Y, verticesPositions[0].Z,
                    verticesPositions[1].X, verticesPositions[1].Y, verticesPositions[1].Z,
                    verticesPositions[2].X, verticesPositions[2].Y, verticesPositions[2].Z,
                    verticesPositions[3].X, verticesPositions[3].Y, verticesPositions[3].Z,
                    verticesPositions[4].X, verticesPositions[4].Y, verticesPositions[4].Z,
                    verticesPositions[5].X, verticesPositions[5].Y, verticesPositions[5].Z,
                };
                var mesh = new Renderer.RenderMesh();
                mesh.Mesh = new MeshManager.OglMesh(_OCTAHEDRAL_MESH_INDICES, floatPos, i);
                mesh.Texture = TextureManager.FALLBACK_TEXTURE;
                mesh.Shader = ShaderManager.MeshShader;
                CurrentBoneMeshes.Add(mesh);
            }
        }

        private static float GetBoneLength(Skeleton s, int index)
        {
            var l = 0.2f;
            var globalFrames = s.Definition.Data.CreateBoneMatrices(true);
            if (index >= globalFrames.Count()) return l;
            var origin = globalFrames[index].Translation;
            // Use the distance between this bone and the next child in the hierarchy to determine its length
            var nearestChild = -1;
            for (int i = index + 1; i < s.Definition.Data.Bones.Count; i++)
            {
                if (s.Definition.Data.Bones[i].Parent != s.Definition.Data.Bones[index]) continue;
                nearestChild = i;
                break;
            }
            if (nearestChild < 0) return l;
            var tail = globalFrames[nearestChild].Translation;
            l = (tail - origin).Length();
            return l;
        }

        private static int[] _OCTAHEDRAL_MESH_INDICES = new int[]
		{
            1, 2, 4,
            4, 2, 3,
            0, 5, 3,
            0, 2, 1,
            3, 2, 0,
            1, 4, 5,
            5, 4, 3,
            5, 0, 1
        };

		private static Vector3[] _OCTAHEDRAL_MESH_POS = new Vector3[]
		{
			new Vector3(0.1f, 0.1f, -0.1f),
            new Vector3(0.1f, -0.1f, -0.1f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0.1f, 0.1f, 0.1f),
            new Vector3(0.1f, -0.1f, 0.1f),
            new Vector3(0f, 0f, 0f),
		};
	}
}