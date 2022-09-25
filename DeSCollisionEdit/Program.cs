using System;
using System.Diagnostics;
using System.Numerics;
using Assimp;
using SoulsFormats;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Debugger.Launch();
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a Demon's Souls .hkx or an .obj with a matching Demon's Souls .hkx in the same folder.");
                return;
            }
            bool bigEndianOut = true; //Only for NavMeshes for now

            string outFmt = "";
            int i = 0;
            for(i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == ('-'))
                {
                    switch(arg)
                    {
                        case "-be":
                            bigEndianOut = true;
                            break;
                        case "-le":
                            bigEndianOut = false;
                            break;
                        //case "-toHKX":
                        //    outFmt = "hkx";
                        //    break;
                        case "-toNvm":
                            outFmt = "nvm";
                            break;
                    }
                } else
                {
                    break;
                }
            }
            var ext = Path.GetExtension(args[0]);
            var ctx = new Assimp.AssimpContext();
            var formats = ctx.GetSupportedExportFormats();
            ExportFormatDescription exformat = formats[0];
            Scene scene;
            Assimp.AssimpContext context = new Assimp.AssimpContext();
            Assimp.Scene aiScene;

            Dictionary<int, Assimp.Mesh> aiMeshes = new Dictionary<int, Mesh>();
            switch (ext)
            {
                case ".nvm":
                    var nvmOut = NVM.Read(args[i]);
                    scene = AssimpNVMExport(args[i], nvmOut);
                    for (int j = 0; j < formats.Length; j++)
                    {
                        var format = formats[j];
                        if (format.FileExtension == "fbx")
                        {
                            exformat = format;
                            break;
                        }
                    }
                    ctx.ExportFile(scene, Path.ChangeExtension(args[0], ".fbx"), exformat.FormatId, Assimp.PostProcessSteps.FlipUVs);
                    break;
                case ".flver": //Assumes DeS Flver for now
                    var flvOut = FLVER0.Read(args[i]);
                    scene = AssimpFLVER0Export(args[i], flvOut);
                    for (int j = 0; j < formats.Length; j++)
                    {
                        var format = formats[j];
                        if (format.FileExtension == "obj")
                        {
                            exformat = format;
                            break;
                        }
                    }
                    ctx.ExportFile(scene, Path.ChangeExtension(args[0], ".obj"), exformat.FormatId, Assimp.PostProcessSteps.FlipUVs);
                    break;
                case ".hkx":
                    var hkxOut = HKX.Read(args[i]);
                    var hkxMeshSet = GetHKXMeshes(hkxOut);
                    scene = AssimpHKXExport(args[i], hkxMeshSet);
                    for(int j = 0; j < formats.Length; j++)
                    {
                        var format = formats[j];
                        if(format.FileExtension == "obj")
                        {
                            exformat = format;
                            break;
                        }
                    }
                    ctx.ExportFile(scene, Path.ChangeExtension(args[0], ".obj"), exformat.FormatId, Assimp.PostProcessSteps.FlipUVs);
                    break;
                default:
                    switch(outFmt)
                    {
                        case "hkx":
                            var hkxPath = Path.ChangeExtension(args[i], ".hkx");
                            context.SetConfig(new Assimp.Configs.FBXPreservePivotsConfig(false));
                            aiScene = context.ImportFile(args[i], Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.FlipUVs);
                            GetAIMeshes(aiMeshes, aiScene, aiScene.RootNode);

                            var hkx = HKX.Read(hkxPath);
                            var hkxMeshes = GetHKXMeshes(hkx);

                            foreach (var pair in hkxMeshes)
                            {
                                if (aiMeshes.ContainsKey(pair.Key))
                                {
                                    var aiMesh = aiMeshes[pair.Key];
                                    AItoHKXMesh(aiMesh, aiScene.Materials[aiMesh.MaterialIndex], pair.Value);
                                }
                            }

                            hkx.Write(hkxPath);
                            break;
                        case "nvm":
                            var nvmPath = Path.ChangeExtension(args[i], ".nvm");
                            context.SetConfig(new Assimp.Configs.FBXPreservePivotsConfig(false));
                            aiScene = context.ImportFile(args[i], Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.FlipUVs);
                            GetAIMeshes(aiMeshes, aiScene, aiScene.RootNode, false);

                            var nvm = AItoNavMesh(aiScene, bigEndianOut);

                            nvm.Write(nvmPath);
                            break;
                    }
                    break;
            }
        }

        public static NVM AItoNavMesh(Assimp.Scene scene, bool bigEndianOut)
        {
            List<List<(int triId, int triVertId)>> faceListByVertexId = new List<List<(int triId, int triVertId)>>();
            var nvm = new NVM();
            nvm.BigEndian = bigEndianOut;
            nvm.Vertices = new List<Vector3>();
            nvm.Triangles = new List<NVM.Triangle>();
            nvm.Entities = new List<NVM.Entity>();

            int globalVertCountTracker = 0;
            
            for(int i = 0; i < scene.MeshCount; i++)
            {
                for(int v = 0; v < scene.Meshes[i].VertexCount; v++)
                {
                    var vertex = scene.Meshes[i].Vertices[v];
                    vertex.X = -vertex.X;
                    scene.Meshes[i].Vertices[v] = vertex;
                }
            }

            //Iniitialize extents to a vertex we know is in our range. Leaving as 0s would be bad if the mesh doesn't intersect there.
            Vector3 rootBoxMinExtents = new Vector3(scene.Meshes[0].Vertices[0].X, scene.Meshes[0].Vertices[0].Y, scene.Meshes[0].Vertices[0].Z);
            Vector3 rootBoxMaxExtents = rootBoxMinExtents;
            for(int i = 0; i < scene.MeshCount; i++)
            {
                var mesh = scene.Meshes[i];
                Dictionary<int, int> vertIndexRemapper = new Dictionary<int, int>(); //For reassigning vertex ids in faces after they've been combined.
                
                for (int v = 0; v < mesh.VertexCount; v++)
                {
                    var vert = mesh.Vertices[v];

                    //Min extents
                    if(rootBoxMinExtents.X > vert.X)
                    {
                        rootBoxMinExtents.X = vert.X;
                    }
                    if (rootBoxMinExtents.Y > vert.Y)
                    {
                        rootBoxMinExtents.Y = vert.Y;
                    }
                    if (rootBoxMinExtents.Z > vert.Z)
                    {
                        rootBoxMinExtents.Z = vert.Z;
                    }

                    //Max extents
                    if (rootBoxMaxExtents.X < vert.X)
                    {
                        rootBoxMaxExtents.X = vert.X;
                    }
                    if (rootBoxMaxExtents.Y < vert.Y)
                    {
                        rootBoxMaxExtents.Y = vert.Y;
                    }
                    if (rootBoxMaxExtents.Z < vert.Z)
                    {
                        rootBoxMaxExtents.Z = vert.Z;
                    }

                    //Combine and remap repeated vertices as needed.
                    //Assimp splits things by material by necessity and so we need to recombine them.
                    var vertData = new Vector3(vert.X, vert.Y, vert.Z);
                    bool foundDuplicateVert = false;
                    for(int vt = 0; vt < nvm.Vertices.Count; vt++)
                    {
                        if(vertData == nvm.Vertices[vt])
                        {
                            foundDuplicateVert = true;
                            vertIndexRemapper.Add(v, vt);
                        }
                    }
                    if(!foundDuplicateVert)
                    {
                        vertIndexRemapper.Add(v, nvm.Vertices.Count);
                        nvm.Vertices.Add(new Vector3(vert.X, vert.Y, vert.Z));
                        faceListByVertexId.Add(new List<(int, int)>());
                        globalVertCountTracker++;
                    }
                }

                GetTriangleMetadata(scene.Materials[mesh.MaterialIndex].Name, out int EntityId, out NVM.TriangleFlags triFlags, out int obstacleCount);

                //Entities - Not used whatsoever in base Demon's Souls so may not be implemented there
                NVM.Entity entity = null;
                if (EntityId != 0)
                {
                    entity = new NVM.Entity();
                    entity.EntityID = EntityId;
                    entity.TriangleIndices = new List<int>();
                }

                for (int f = 0; f < mesh.FaceCount; f++)
                {
                    var face = mesh.Faces[f];
                    var tri = new NVM.Triangle();

                    //Default edges to -1
                    tri.EdgeIndex1 = -1;
                    tri.EdgeIndex2 = -1;
                    tri.EdgeIndex3 = -1;

                    //Ensure we remap vert indices to their new, combined ids as needed
                    tri.VertexIndex1 = vertIndexRemapper.ContainsKey(face.Indices[0]) ? vertIndexRemapper[face.Indices[0]] : face.Indices[0];
                    tri.VertexIndex2 = vertIndexRemapper.ContainsKey(face.Indices[2]) ? vertIndexRemapper[face.Indices[2]] : face.Indices[2];
                    tri.VertexIndex3 = vertIndexRemapper.ContainsKey(face.Indices[1]) ? vertIndexRemapper[face.Indices[1]] : face.Indices[1];

                    tri.Flags = triFlags;
                    tri.ObstacleCount = obstacleCount;

                    if(entity != null)
                    {
                        entity.TriangleIndices.Add(nvm.Triangles.Count);
                    }
                    faceListByVertexId[tri.VertexIndex1].Add((nvm.Triangles.Count, 1));
                    faceListByVertexId[tri.VertexIndex2].Add((nvm.Triangles.Count, 2));
                    faceListByVertexId[tri.VertexIndex3].Add((nvm.Triangles.Count, 3));
                    nvm.Triangles.Add(tri);
                }

                if(entity != null)
                {
                    if (nvm.Entities == null)
                    {
                        nvm.Entities = new List<NVM.Entity>();
                    }
                    nvm.Entities.Add(entity);
                }
            }

            //Loop back through faces and fix tri edge links
            //Check through faceListByVertexId and assign based on if a second vertex matches and the third vertex does not match (ie same face or a copy)
            for(int i = 0; i < nvm.Triangles.Count; i++)
            {
                var tri = nvm.Triangles[i];
                CheckEdgesByVertex(faceListByVertexId, nvm, tri, 1);
                CheckEdgesByVertex(faceListByVertexId, nvm, tri, 2);
                CheckEdgesByVertex(faceListByVertexId, nvm, tri, 3);

            }

            //Boxes - Bounding boxes that on the last stages of their trees can contain tri indices. Each subdivides into 4 sections and typically these only go 3-4 levels deeper
            nvm.RootBox = GenerateNVMBoundingBox(nvm.Vertices, faceListByVertexId, rootBoxMinExtents, rootBoxMaxExtents, 0, 4);

            return nvm;
        }

        //Since we've already gathered tris by vertex, search through them by a vertex id from the current triangle
        private static void CheckEdgesByVertex(List<List<(int triId, int triVertId)>> faceListByVertexId, NVM nvm, NVM.Triangle tri, int triVertexIndex)
        {
            int vertToCheck = -1;
            switch(triVertexIndex)
            {
                case 1:
                    vertToCheck = tri.VertexIndex1;
                    break;
                case 2:
                    vertToCheck = tri.VertexIndex2;
                    break;
                case 3:
                    vertToCheck = tri.VertexIndex3;
                    break;
            }
            foreach (var vertTriData in faceListByVertexId[vertToCheck])
            {
                var verTri = nvm.Triangles[vertTriData.triId];
                bool triVert1Match = false;
                bool triVert2Match = false;
                bool triVert3Match = false;

                //We know 1 vert matches so check the ones we haven't checked yet
                switch (vertTriData.triVertId)
                {
                    case 1:
                        triVert2Match = tri.VertexIndex2 == verTri.VertexIndex2 || tri.VertexIndex2 == verTri.VertexIndex3;
                        triVert3Match = tri.VertexIndex3 == verTri.VertexIndex2 || tri.VertexIndex3 == verTri.VertexIndex3;

                        if (triVert2Match && triVert3Match)
                        {
                            continue;
                        }
                        if (triVert2Match)
                        {
                            tri.EdgeIndex1 = vertTriData.triId;
                        }
                        if (triVert3Match)
                        {
                            tri.EdgeIndex3 = vertTriData.triId;
                        }
                        break;
                    case 2:
                        triVert1Match = tri.VertexIndex1 == verTri.VertexIndex1 || tri.VertexIndex1 == verTri.VertexIndex3;
                        triVert3Match = tri.VertexIndex3 == verTri.VertexIndex2 || tri.VertexIndex3 == verTri.VertexIndex3;

                        if (triVert1Match && triVert3Match)
                        {
                            continue;
                        }
                        if (triVert1Match)
                        {
                            tri.EdgeIndex1 = vertTriData.triId;
                        }
                        if (triVert3Match)
                        {
                            tri.EdgeIndex2 = vertTriData.triId;
                        }
                        break;
                    case 3:
                        triVert1Match = tri.VertexIndex1 == verTri.VertexIndex1 || tri.VertexIndex1 == verTri.VertexIndex3;
                        triVert2Match = tri.VertexIndex2 == verTri.VertexIndex2 || tri.VertexIndex2 == verTri.VertexIndex3;

                        if (triVert1Match && triVert2Match)
                        {
                            continue;
                        }
                        if (triVert1Match)
                        {
                            tri.EdgeIndex3 = vertTriData.triId;
                        }
                        if (triVert2Match)
                        {
                            tri.EdgeIndex2 = vertTriData.triId;
                        }
                        break;
                }
            }
        }

        private static NVM.Box GenerateNVMBoundingBox(List<Vector3> vertices, List<List<(int triId, int triVertId)>> faceListByVertexId, Vector3 boxMinExtents, Vector3 boxMaxExtents, int currentDepth, int maxDepth)
        {
            NVM.Box box = new NVM.Box();
            box.MinValueCorner = boxMinExtents;
            box.MaxValueCorner = boxMaxExtents;
            box.TriangleIndices = new List<int>();

            //If we're below the max depth, subdivide. Otherwise, gather all triangle ids that make sense.
            if(currentDepth < maxDepth)
            {
                double halfExtentX = ((double)boxMaxExtents.X) - ((double)boxMinExtents.X);
                double halfExtentZ = ((double)boxMaxExtents.Z) - ((double)boxMinExtents.Z);
                float midwayX = (float)(halfExtentX + boxMinExtents.X);
                float midwayZ = (float)(halfExtentZ + boxMinExtents.Z);

                Vector3 box1Min = boxMinExtents;
                Vector3 box1Max = new Vector3(midwayX, boxMaxExtents.Y, midwayZ);

                Vector3 box2Min = new Vector3(midwayX, boxMinExtents.Y, boxMinExtents.Z);
                Vector3 box2Max = new Vector3(boxMaxExtents.X, boxMaxExtents.Y, midwayZ);

                Vector3 box3Min = new Vector3(midwayX, boxMinExtents.Y, midwayZ);
                Vector3 box3Max = boxMaxExtents;

                Vector3 box4Min = new Vector3(boxMinExtents.X, boxMinExtents.Y, midwayZ);
                Vector3 box4Max = new Vector3(midwayX, boxMaxExtents.Y, boxMaxExtents.Z);

                currentDepth++;
                box.ChildBox1 = GenerateNVMBoundingBox(vertices, faceListByVertexId, box1Min, box1Max, currentDepth, maxDepth);
                box.ChildBox2 = GenerateNVMBoundingBox(vertices, faceListByVertexId, box2Min, box2Max, currentDepth, maxDepth);
                box.ChildBox3 = GenerateNVMBoundingBox(vertices, faceListByVertexId, box3Min, box3Max, currentDepth, maxDepth);
                box.ChildBox4 = GenerateNVMBoundingBox(vertices, faceListByVertexId, box4Min, box4Max, currentDepth, maxDepth);
            } else
            {
                box.ChildBox1 = null;
                box.ChildBox2 = null;
                box.ChildBox3 = null;
                box.ChildBox4 = null;

                for(int i = 0; i < vertices.Count; i++)
                {
                    var vert = vertices[i];

                    //Check if this vertex is within the bounds of the bounding box. If it is, we include all faces it's used in
                    if (vert.X >= boxMinExtents.X && vert.X <= boxMaxExtents.X && vert.Y >= boxMinExtents.Y && vert.Y <= boxMaxExtents.Y && vert.Z >= boxMinExtents.Z && vert.Z <= boxMaxExtents.Z)
                    {
                        foreach(var triData in faceListByVertexId[i])
                        {
                            if(!box.TriangleIndices.Contains(triData.triId))
                            {
                                box.TriangleIndices.Add(triData.triId);
                            }
                        }
                    } else
                    {
                        continue;
                    }
                }
            }

            return box;
        }

        //Triangle Flags and Entity ids will be determined by the material name. 
        //@ - EntityId  - EventId that disables the attached triangles
        //# - TriangleFlags - Special flags that can be placed to denote special areas like ladders, doors, and other funny areas.
        //% - ObstacleCount - Number of breakable objects on triangle? Probably going to be awkward to set normally.
        //Ex. @1501#GATE#LADDER#EVENT%3
        private static void GetTriangleMetadata(string matName, out int entityId, out NVM.TriangleFlags triFlags, out int obstacleCount)
        {
            entityId = 0;
            triFlags = NVM.TriangleFlags.NONE;
            obstacleCount = 0;
            if(matName == null || matName == "")
            {
                return;
            }

            var entitySplit = matName.Split('@');
            if(entitySplit.Length > 1)
            {
                var entityFinalSplit = entitySplit[1].Split('#', '%');
                Int32.TryParse(entityFinalSplit[0], out entityId);
            }

            var flagsSplit = matName.Split('#');
            if (flagsSplit.Length > 1)
            {
                for(int i = 1; i < flagsSplit.Length; i++)
                {
                    var flagsFinalSplit = flagsSplit[i].Split('@', '%');
                    var flag = (NVM.TriangleFlags)Enum.Parse(typeof(NVM.TriangleFlags), flagsFinalSplit[0], true);
                    triFlags |= flag;
                }

            }

            var obstacleSplit = matName.Split('%');
            if (obstacleSplit.Length > 1)
            {
                var obstacleFinalSplit = obstacleSplit[1].Split('#', '@');
                Int32.TryParse(obstacleFinalSplit[0], out obstacleCount);
            }
        }

        public static Assimp.Scene AssimpNVMExport(string filePath, NVM nvm)
        {
            var mirrorMat = new System.Numerics.Matrix4x4(-1, 0, 0, 0,
                                        0, 1, 0, 0,
                                        0, 0, 1, 0,
                                        0, 0, 0, 1);
            Assimp.Scene aiScene = new Assimp.Scene();

            //Create an array to hold references to these since Assimp lacks a way to grab these by order or id
            //We don't need the nodo count in this since they can't be parents
            Assimp.Node[] boneArray = new Assimp.Node[2];

            //Set up root node
            var aiRootNode = new Assimp.Node("RootNode", null);
            aiRootNode.Transform = Assimp.Matrix4x4.Identity;

            boneArray[0] = aiRootNode;
            aiScene.RootNode = aiRootNode;

            //Set up single child node
            var aiNode = new Assimp.Node(Path.GetFileNameWithoutExtension(filePath) + "_node", aiRootNode);

            //Get local transform
            aiNode.Transform = aiRootNode.Transform;

            aiRootNode.Children.Add(aiNode);
            boneArray[1] = aiNode;

            //Separate out to meshes by flag combos
            int i = 0;
            Dictionary<string, List<NVM.Triangle>> meshDict = new Dictionary<string, List<NVM.Triangle>>(); 
            for (int triId = 0; triId < nvm.Triangles.Count; triId++)
            {
                var tri = nvm.Triangles[triId];
                string name = $"mat";

                //Entities
                foreach(var ent in nvm.Entities)
                {
                    if(ent.TriangleIndices.Contains(triId))
                    {
                        name += $"@{ent.EntityID}";
                    }
                }
                
                //Flags
                foreach(var flag in Enum.GetValues(typeof(NVM.TriangleFlags)))
                {
                    if ((tri.Flags & (NVM.TriangleFlags)flag) > 0)
                    {
                        name += $"#{flag}";
                    }
                }

                //Obstacle Count
                if(tri.ObstacleCount > 0)
                {
                    name += $"%{tri.ObstacleCount}";
                }

                if(!meshDict.ContainsKey(name))
                {
                    meshDict.Add(name, new List<NVM.Triangle>());
                }
                meshDict[name].Add(tri);

                i++;
            }

            //Assemble Meshes
            int m = 0;
            foreach(var pair in meshDict)
            {
                Dictionary<int, int> vertIndexRemap = new Dictionary<int, int>();
                var mesh = new Assimp.Mesh();
                var mat = new Assimp.Material();
                mesh.Name = $"mesh_{m}";
                mat.Name = pair.Key;
                mat.ColorDiffuse = new Assimp.Color4D(1, 1, 1, 1);
                mat.ShadingMode = Assimp.ShadingMode.Phong;

                foreach(var tri in pair.Value)
                {
                    if(!vertIndexRemap.ContainsKey(tri.VertexIndex1))
                    {
                        vertIndexRemap.Add(tri.VertexIndex1, mesh.Vertices.Count);
                        var vert0 = Vector3.Transform(nvm.Vertices[tri.VertexIndex1], mirrorMat);
                        mesh.Vertices.Add(new Vector3D(vert0.X, vert0.Y, vert0.Z));
                    }
                    if (!vertIndexRemap.ContainsKey(tri.VertexIndex2))
                    {
                        vertIndexRemap.Add(tri.VertexIndex2, mesh.Vertices.Count);
                        var vert1 = Vector3.Transform(nvm.Vertices[tri.VertexIndex2], mirrorMat);
                        mesh.Vertices.Add(new Vector3D(vert1.X, vert1.Y, vert1.Z));
                    }
                    if (!vertIndexRemap.ContainsKey(tri.VertexIndex3))
                    {
                        vertIndexRemap.Add(tri.VertexIndex3, mesh.Vertices.Count);
                        var vert2 = Vector3.Transform(nvm.Vertices[tri.VertexIndex3], mirrorMat);
                        mesh.Vertices.Add(new Vector3D(vert2.X, vert2.Y, vert2.Z));
                    }
                    mesh.Faces.Add(new Assimp.Face(new int[] { vertIndexRemap[tri.VertexIndex1], vertIndexRemap[tri.VertexIndex3], vertIndexRemap[tri.VertexIndex2] }));
                }

                //Handle rigid meshes
                {
                    var aiBone = new Assimp.Bone();
                    var aqnBone = boneArray[0];

                    // Name
                    aiBone.Name = aiNode.Name;

                    // VertexWeights
                    for (int vw = 0; vw < mesh.Vertices.Count; vw++)
                    {
                        var aiVertexWeight = new Assimp.VertexWeight(vw, 1f);
                        aiBone.VertexWeights.Add(aiVertexWeight);
                    }

                    aiBone.OffsetMatrix = Assimp.Matrix4x4.Identity;

                    mesh.Bones.Add(aiBone);
                }

                mesh.MaterialIndex = m;
                aiScene.Materials.Add(mat);
                aiScene.Meshes.Add(mesh);

                // Set up mesh node and add this mesh's index to it (This tells assimp to export it as a mesh for various formats)
                string meshNodeName = $"mesh_{m}";
                var meshNode = new Assimp.Node(meshNodeName, aiScene.RootNode);
                meshNode.Transform = Assimp.Matrix4x4.Identity;

                aiScene.RootNode.Children.Add(meshNode);

                meshNode.MeshIndices.Add(aiScene.Meshes.Count - 1);
                m++;
            }

            return aiScene;
        }

        public static Assimp.Scene AssimpFLVER0Export(string filePath, FLVER0 flv)
        {
            var mirrorMat = new System.Numerics.Matrix4x4(-1, 0, 0, 0,
                                        0, 1, 0, 0,
                                        0, 0, 1, 0,
                                        0, 0, 0, 1);
            Assimp.Scene aiScene = new Assimp.Scene();

            //Create an array to hold references to these since Assimp lacks a way to grab these by order or id
            //We don't need the nodo count in this since they can't be parents
            Assimp.Node[] boneArray = new Assimp.Node[2];

            //Set up root node
            var aiRootNode = new Assimp.Node("RootNode", null);
            aiRootNode.Transform = Assimp.Matrix4x4.Identity;

            boneArray[0] = aiRootNode;
            aiScene.RootNode = aiRootNode;

            //Set up single child node
            var aiNode = new Assimp.Node(Path.GetFileNameWithoutExtension(filePath) + "_node", aiRootNode);

            //Get local transform
            aiNode.Transform = aiRootNode.Transform;

            aiRootNode.Children.Add(aiNode);
            boneArray[1] = aiNode;

            //Mesh
            int i = 0;
            foreach (var mesh in flv.Meshes)
            {
                var defaultBoneMatrix = flv.ComputeBoneWorldMatrix(mesh.DefaultBoneIndex);

                //Transform based on root
                for (int v = 0; v < mesh.Vertices.Count; v++)
                {
                    var vert = mesh.Vertices[v];
                    vert.Position = Vector3.Transform(vert.Position, defaultBoneMatrix);
                    vert.Position = Vector3.Transform(vert.Position, mirrorMat);
                    vert.Normal = Vector3.TransformNormal(vert.Normal, defaultBoneMatrix);
                    vert.Normal = Vector3.TransformNormal(vert.Normal, mirrorMat);
                    mesh.Vertices[v] = vert;
                }
                var aiMesh = new Assimp.Mesh($"mesh_{i}", Assimp.PrimitiveType.Triangle);
                var verts = mesh.Vertices;
                for (int vertId = 0; vertId < verts.Count; vertId++)
                {
                    var vert = verts[vertId];

                    var pos = vert.Position;
                    var nrm = vert.Normal;
                    aiMesh.Vertices.Add(new Assimp.Vector3D(pos.X, pos.Y, pos.Z));
                    aiMesh.Normals.Add(new Assimp.Vector3D(nrm.X, nrm.Y, nrm.Z));
                }

                //Handle rigid meshes
                {
                    var aiBone = new Assimp.Bone();
                    var aqnBone = boneArray[0];

                    // Name
                    aiBone.Name = aiNode.Name;

                    // VertexWeights
                    for (int vw = 0; vw < aiMesh.Vertices.Count; vw++)
                    {
                        var aiVertexWeight = new Assimp.VertexWeight(vw, 1f);
                        aiBone.VertexWeights.Add(aiVertexWeight);
                    }

                    aiBone.OffsetMatrix = Assimp.Matrix4x4.Identity;

                    aiMesh.Bones.Add(aiBone);
                }

                //Faces
                var indices = mesh.Triangulate(((FLVER0)flv).Header.Version);
                for (int id = 0; id < indices.Count - 2; id += 3)
                {
                    aiMesh.Faces.Add(new Assimp.Face(new int[] { (int)indices[id], (int)indices[id + 2], (int)indices[id + 1] }));
                }

                //Material
                Assimp.Material mate = new Assimp.Material();

                mate.ColorDiffuse = new Assimp.Color4D(1, 1, 1, 1);

                mate.Name = $"mate_{7}";

                mate.ShadingMode = Assimp.ShadingMode.Phong;

                var meshNodeName = Path.GetFileNameWithoutExtension(filePath);

                // Add mesh to meshes
                aiScene.Meshes.Add(aiMesh);

                // Add material to materials
                aiScene.Materials.Add(mate);

                // MaterialIndex
                aiMesh.MaterialIndex = aiScene.Materials.Count - 1;

                // Set up mesh node and add this mesh's index to it (This tells assimp to export it as a mesh for various formats)
                var meshNode = new Assimp.Node(meshNodeName, aiScene.RootNode);
                meshNode.Transform = Assimp.Matrix4x4.Identity;

                aiScene.RootNode.Children.Add(meshNode);

                meshNode.MeshIndices.Add(aiScene.Meshes.Count - 1);
                i++;
            }

            return aiScene;
        }

        public static Assimp.Scene AssimpHKXExport(string filePath, Dictionary<int, HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage> hkxMeshDict)
        {
            var mirrorMat = new System.Numerics.Matrix4x4(-1, 0, 0, 0,
                                        0, 1, 0, 0,
                                        0, 0, 1, 0,
                                        0, 0, 0, 1);
            Assimp.Scene aiScene = new Assimp.Scene();

            //Create an array to hold references to these since Assimp lacks a way to grab these by order or id
            //We don't need the nodo count in this since they can't be parents
            Assimp.Node[] boneArray = new Assimp.Node[2];

            //Set up root node
            var aiRootNode = new Assimp.Node("RootNode", null);
            aiRootNode.Transform = Assimp.Matrix4x4.Identity;

            boneArray[0] = aiRootNode;
            aiScene.RootNode = aiRootNode;

            //Set up single child node
            var aiNode = new Assimp.Node(Path.GetFileNameWithoutExtension(filePath) + "_node", aiRootNode);

            //Get local transform
            aiNode.Transform = aiRootNode.Transform;

            aiRootNode.Children.Add(aiNode);
            boneArray[1] = aiNode;

            //Mesh
            foreach(var hkxMesh in hkxMeshDict)
            {
                var aiMesh = new Assimp.Mesh($"mesh_{hkxMesh.Key}", Assimp.PrimitiveType.Triangle);

                var verts = hkxMesh.Value.Vertices.GetArrayData().Elements;
                for (int vertId = 0; vertId < verts.Count; vertId++)
                {
                    var vert = verts[vertId];

                    var pos = new Vector3(vert.Vector.X, vert.Vector.Y, vert.Vector.Z);
                    pos = Vector3.Transform(pos, mirrorMat);
                    aiMesh.Vertices.Add(new Assimp.Vector3D(pos.X, pos.Y, pos.Z));

                }

                //Handle rigid meshes
                {
                    var aiBone = new Assimp.Bone();
                    var aqnBone = boneArray[0];

                    // Name
                    aiBone.Name = aiNode.Name;

                    // VertexWeights
                    for (int i = 0; i < aiMesh.Vertices.Count; i++)
                    {
                        var aiVertexWeight = new Assimp.VertexWeight(i, 1f);
                        aiBone.VertexWeights.Add(aiVertexWeight);
                    }

                    aiBone.OffsetMatrix = Assimp.Matrix4x4.Identity;

                    aiMesh.Bones.Add(aiBone);
                }

                //Faces
                dynamic indices;
                if (hkxMesh.Value.Indices8?.Capacity > 0)
                {
                    indices = hkxMesh.Value.Indices8.GetArrayData().Elements;
                }
                else if (hkxMesh.Value.Indices16?.Capacity > 0)
                {
                    indices = hkxMesh.Value.Indices16.GetArrayData().Elements;
                }
                else //Indices32 have to be there if those aren't
                {
                    indices = hkxMesh.Value.Indices32.GetArrayData().Elements;
                }
                for (int i = 0; i < indices.Count; i+=4)
                {
                    aiMesh.Faces.Add(new Assimp.Face(new int[] { (int)indices[i].data, (int)indices[i + 2].data, (int)indices[i + 1].data }));
                }

                //Material
                Assimp.Material mate = new Assimp.Material();

                mate.ColorDiffuse = new Assimp.Color4D(1, 1, 1, 1);
                if(hkxMesh.Value.Materials?.Capacity > 0)
                {
                    mate.Name = $"mate_{hkxMesh.Value.Materials[0].data}";
                } else
                {
                    mate.Name = $"mate_{7}";
                }

                mate.ShadingMode = Assimp.ShadingMode.Phong;

                var meshNodeName = Path.GetFileNameWithoutExtension(filePath);

                // Add mesh to meshes
                aiScene.Meshes.Add(aiMesh);

                // Add material to materials
                aiScene.Materials.Add(mate);

                // MaterialIndex
                aiMesh.MaterialIndex = aiScene.Materials.Count - 1;

                // Set up mesh node and add this mesh's index to it (This tells assimp to export it as a mesh for various formats)
                var meshNode = new Assimp.Node(meshNodeName, aiScene.RootNode);
                meshNode.Transform = Assimp.Matrix4x4.Identity;

                aiScene.RootNode.Children.Add(meshNode);

                meshNode.MeshIndices.Add(aiScene.Meshes.Count - 1);
            }

            return aiScene;
        }

        private static void AItoHKXMesh(Assimp.Mesh aiMesh, Assimp.Material mat, HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage hkxMesh)
        {
            List<HKX.HKVector4> hkxVerts = new List<HKX.HKVector4>();
            for(int i = 0; i < aiMesh.VertexCount; i++)
            {
                var vert = aiMesh.Vertices[i];
                hkxVerts.Add(new HKX.HKVector4() { Vector = new System.Numerics.Vector4(vert.X, vert.Y, -vert.Z, 0) });
            }
            hkxMesh.Vertices.SetArray(hkxVerts);

            List<HKX.HKUShort> hkxUshorts = new List<HKX.HKUShort>();
            for (int i = 0; i < aiMesh.FaceCount; i++)
            {
                var face = aiMesh.Faces[i];
                hkxUshorts.Add(new HKX.HKUShort() { data = (ushort)face.Indices[0] });
                hkxUshorts.Add(new HKX.HKUShort() { data = (ushort)face.Indices[2] });
                hkxUshorts.Add(new HKX.HKUShort() { data = (ushort)face.Indices[1] });
                hkxUshorts.Add(new HKX.HKUShort() { data = 0xCDCD });
            }
            hkxMesh.Indices16.SetArray(hkxUshorts);

            var matIndices = new List<HKX.HKByte>();
            for(int id = 0; id < matIndices.Count; id++)
            {
                matIndices.Add(new HKX.HKByte() { data = 0});
            }
            hkxMesh.MaterialIndices.SetArray(matIndices);
            hkxMesh.Materials.SetArray(new List<HKX.HKUInt>(new HKX.HKUInt[1] { new HKX.HKUInt() { data = (uint)GetMeshNum(mat.Name)} }));
        }

        private static Dictionary<int, HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage> GetHKXMeshes(HKX hkx)
        {
            Dictionary<int, HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage> meshDict = new Dictionary<int, HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage>();

            for(int i = 0; i < hkx.DataSection.Objects.Count; i++)
            {
                var obj = hkx.DataSection.Objects[i];
                if(obj is HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage)
                {
                    var meshObj = (HKX.HKPStorageExtendedMeshShapeMeshSubpartStorage)obj;
                    meshDict.Add(i, meshObj);
                }
            }

            return meshDict;

        }

        private static Dictionary<int, Assimp.Mesh> GetAIMeshes(Dictionary<int, Assimp.Mesh> meshDict, Assimp.Scene aiScene, Assimp.Node node, bool useMeshNameAsNum = true)
        {
            foreach (int meshId in node.MeshIndices)
            {
                var mesh = aiScene.Meshes[meshId];
                if(useMeshNameAsNum)
                {
                    meshDict.Add(GetMeshNum(mesh.Name), mesh);
                } else
                {
                    meshDict.Add(meshId, mesh);
                }
            }

            foreach (var childNode in node.Children)
            {
                GetAIMeshes(meshDict, aiScene, childNode, useMeshNameAsNum);
            }

            return meshDict;
        }

        private static int GetMeshNum(string meshName)
        {
            var split = meshName.Split('_');

            return Int32.Parse(split[split.Length - 1]);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Example usage:");
            Console.WriteLine("");
            Console.WriteLine("DeSCollisionEdit.exe example.hkx");
            Console.WriteLine("  Takes in an .hkx file and dumps the meshes to a single .obj file.");
            Console.WriteLine("DeSCollisionEdit.exe example.obj");
            Console.WriteLine("  Takes in an .obj file and replaces vertices and faces while dummying materials. The collision .hkx it came from should be in the same folder.");
        }
    }
}