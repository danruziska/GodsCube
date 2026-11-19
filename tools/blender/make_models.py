"""Gera os modelos do Cubo dos Deuses: Piece.fbx (a peca) e Core.fbx (o nucleo).
Uso:  blender --background --python tools/blender/make_models.py -- <pasta_de_saida>
"""
import sys, os
import bpy

# nome, (x, y, z) no Blender, largura do bevel, segmentos
# O eixo FINO da peca e o Y do Blender: com Forward -Z / Up Y ele vira o Z do Unity.
MODELS = [
    ("Piece", (0.9, 0.1, 0.9), 0.03, 3),
    ("Core",  (1.0, 1.0, 1.0), 0.02, 2),     # lado 1: o Unity escala para o tamanho do cubo
]
out_dir = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.getcwd()


def build(name, size, bevel, segments):
    # 1. cena vazia
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # 2. cubo de lado 1, escalado para o tamanho final, com a escala aplicada
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name + "Mesh"
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # 3. bevel como modificador, aplicado
    mod = obj.modifiers.new("Bevel", 'BEVEL')
    mod.width = bevel
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    bpy.ops.object.modifier_apply(modifier="Bevel")

    # 4. sombreado suave, com as faces grandes planas (Weighted Normal)
    bpy.ops.object.shade_smooth()
    wn = obj.modifiers.new("WeightedNormal", 'WEIGHTED_NORMAL')
    wn.keep_sharp = True
    wn.weight = 50
    bpy.ops.object.modifier_apply(modifier="WeightedNormal")

    # 5. origem no centro da geometria
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    obj.location = (0, 0, 0)

    # 6. relatorio
    me = obj.data
    me.calc_loop_triangles()
    dims = [max(v.co[i] for v in me.vertices) - min(v.co[i] for v in me.vertices) for i in range(3)]
    print("MODEL %s size = (%.3f, %.3f, %.3f) tris = %d scale = %s"
          % (name, dims[0], dims[1], dims[2], len(me.loop_triangles), tuple(round(s, 3) for s in obj.scale)))

    # 7. exporta so este objeto
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    path = os.path.join(out_dir, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        object_types={'MESH'}, mesh_smooth_type='OFF', add_leaf_bones=False)
    print("MODEL %s fbx = %s" % (name, path))


for model in MODELS:
    build(*model)