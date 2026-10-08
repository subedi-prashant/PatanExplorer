import os
import sys

import bpy

BODY_GROUPS = {
    "BodyPaint": ({"Paint"}, 40000),
    "BodyDark": ({"Black", "Grid"}, 22000),
    "BodyCarbon": ({"Carbon"}, 7000),
    "BodyInterior": ({"Alcantara", "Cockpit", "CockpitStuff", "Dials", "InteriorBlack", "Leather"}, 14000),
    "BodyGlass": ({"Glass", "Glass.front", "GlassBack", "RearLightsGlass", "Windows"}, 4000),
    "BodyLights": ({"Headlight", "hl inside", "ll shell", "RearLights"}, 6000),
    "BodyLogos": ({"Logos"}, 1000),
    "BodyMechanics": ({"Mechanics", "Mirror"}, 7000),
}

WHEELS = {
    "WheelFrontLeft": "Tire.001_22_45",
    "WheelRearLeft": "Tire.002_26_56",
    "WheelFrontRight": "Tire.003_30_67",
    "WheelRearRight": "Tire.004_34_78",
}

TEXTURES = {
    "Image_9": ("FerrariPaint_BaseColor.png", 1024),
    "Image_4": ("FerrariLogos_BaseColor.png", 512),
    "Image_6": ("FerrariMechanics_BaseColor.png", 512),
    "Image_7": ("FerrariLights_BaseColor.png", 512),
    "Image_10": ("FerrariCarbon_Normal.png", 512),
    "Image_16": ("FerrariTires_BaseColor.png", 1024),
}


def GetArguments():
    separatorIndex = sys.argv.index("--")
    arguments = sys.argv[separatorIndex + 1:]
    if len(arguments) != 3:
        raise ValueError("Expected source GLB, output FBX, and output texture directory.")

    return arguments


def ClearScene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def GetMaterialName(item):
    if len(item.material_slots) == 0 or item.material_slots[0].material is None:
        return ""

    return item.material_slots[0].material.name


def HasAncestor(item, ancestors):
    parent = item.parent
    while parent is not None:
        if parent in ancestors:
            return True

        parent = parent.parent

    return False


def DuplicateAndJoin(items, name, targetTriangleCount):
    duplicates = []
    for item in items:
        worldMatrix = item.matrix_world.copy()
        duplicate = item.copy()
        duplicate.data = item.data.copy()
        bpy.context.scene.collection.objects.link(duplicate)
        duplicate.parent = None
        duplicate.matrix_world = worldMatrix
        duplicates.append(duplicate)

    bpy.ops.object.select_all(action="DESELECT")
    for duplicate in duplicates:
        duplicate.select_set(True)

    result = duplicates[0]
    bpy.context.view_layer.objects.active = result
    if len(duplicates) > 1:
        bpy.ops.object.join()

    result.name = name
    result.data.name = f"{name}Mesh"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    sourceMaterial = items[0].material_slots[0].material if len(items[0].material_slots) > 0 else None
    result.data.materials.clear()
    if sourceMaterial is not None:
        result.data.materials.append(sourceMaterial)

    for polygon in result.data.polygons:
        polygon.material_index = 0
        polygon.use_smooth = True

    result.data.calc_loop_triangles()
    sourceTriangleCount = len(result.data.loop_triangles)
    if sourceTriangleCount > targetTriangleCount:
        modifier = result.modifiers.new(name="WebOptimization", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = targetTriangleCount / sourceTriangleCount
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = result
        bpy.ops.object.modifier_apply(modifier=modifier.name)

    result.data.validate(clean_customdata=False)
    result.data.update()
    return result


def ParentKeepingWorld(item, parent):
    worldMatrix = item.matrix_world.copy()
    item.parent = parent
    item.matrix_world = worldMatrix


def ExportTextures(textureDirectory):
    os.makedirs(textureDirectory, exist_ok=True)
    for imageName, textureSettings in TEXTURES.items():
        outputName, maximumSize = textureSettings
        image = bpy.data.images.get(imageName)
        if image is None:
            raise ValueError(f"Required image is missing: {imageName}")

        width, height = image.size
        largestDimension = max(width, height)
        if largestDimension > maximumSize:
            scale = maximumSize / largestDimension
            image.scale(max(1, round(width * scale)), max(1, round(height * scale)))

        image.filepath_raw = os.path.join(textureDirectory, outputName)
        image.file_format = "PNG"
        image.save()


def ExportModel(outputPath, outputRoot):
    os.makedirs(os.path.dirname(outputPath), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    outputRoot.select_set(True)
    for item in outputRoot.children_recursive:
        item.select_set(True)

    bpy.context.view_layer.objects.active = outputRoot
    bpy.ops.export_scene.fbx(
        filepath=outputPath,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        axis_forward="-Z",
        axis_up="Y",
    )


def CountTriangles(items):
    triangleCount = 0
    for item in items:
        if item.type != "MESH":
            continue

        item.data.calc_loop_triangles()
        triangleCount += len(item.data.loop_triangles)

    return triangleCount


def Main():
    sourcePath, outputPath, textureDirectory = GetArguments()
    ClearScene()
    bpy.ops.import_scene.gltf(filepath=sourcePath)

    wheelSources = {}
    for wheelName, sourceName in WHEELS.items():
        source = bpy.data.objects.get(sourceName)
        if source is None:
            raise ValueError(f"Required wheel hierarchy is missing: {sourceName}")

        wheelSources[wheelName] = source

    sourceMeshes = [item for item in bpy.context.scene.objects if item.type == "MESH"]
    wheelSourceSet = set(wheelSources.values())
    bodyMeshes = [
        item
        for item in sourceMeshes
        if not HasAncestor(item, wheelSourceSet) and GetMaterialName(item) not in {"Material", "Springs"}
    ]

    outputRoot = bpy.data.objects.new("FerrariF40", None)
    bpy.context.scene.collection.objects.link(outputRoot)

    assignedMeshes = set()
    outputMeshes = []
    for groupName, groupSettings in BODY_GROUPS.items():
        materialNames, targetTriangleCount = groupSettings
        groupItems = [item for item in bodyMeshes if GetMaterialName(item) in materialNames]
        if len(groupItems) == 0:
            raise ValueError(f"No source meshes found for {groupName}")

        assignedMeshes.update(groupItems)
        output = DuplicateAndJoin(groupItems, groupName, targetTriangleCount)
        ParentKeepingWorld(output, outputRoot)
        outputMeshes.append(output)

    unassignedMeshes = [item.name for item in bodyMeshes if item not in assignedMeshes]
    if len(unassignedMeshes) > 0:
        raise ValueError(f"Unassigned body meshes: {', '.join(unassignedMeshes)}")

    for wheelName, source in wheelSources.items():
        wheelRoot = bpy.data.objects.new(wheelName, None)
        bpy.context.scene.collection.objects.link(wheelRoot)
        wheelRoot.location = source.matrix_world.translation
        wheelRoot.parent = outputRoot

        descendants = [item for item in source.children_recursive if item.type == "MESH"]
        tireItems = [item for item in descendants if GetMaterialName(item) == "Tires"]
        rimItems = [item for item in descendants if item.parent is not None and item.parent.parent is not None and item.parent.parent.name.startswith("Rims")]
        if len(tireItems) != 1 or len(rimItems) != 1:
            raise ValueError(f"Unexpected wheel hierarchy for {wheelName}")

        tire = DuplicateAndJoin(tireItems, f"{wheelName}_Tire", 5000)
        rim = DuplicateAndJoin(rimItems, f"{wheelName}_Rim", 7000)
        ParentKeepingWorld(tire, wheelRoot)
        ParentKeepingWorld(rim, wheelRoot)
        outputMeshes.extend([tire, rim])

    ExportTextures(textureDirectory)
    ExportModel(outputPath, outputRoot)

    triangleCount = CountTriangles(outputMeshes)
    print(f"PATAN_FERRARI_OUTPUT_TRIANGLES {triangleCount}")
    print(f"PATAN_FERRARI_OUTPUT_MESHES {len(outputMeshes)}")
    print(f"PATAN_FERRARI_OUTPUT_PATH {outputPath}")


Main()
