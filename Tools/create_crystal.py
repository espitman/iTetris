import bpy, os, math
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
# A broad front face, chamfered into four distinct light-catching facets.
verts = [(-.48,-.48,-.18),(.48,-.48,-.18),(.48,.48,-.18),(-.48,.48,-.18),
         (-.35,-.35,.22),(.35,-.35,.22),(.35,.35,.22),(-.35,.35,.22)]
faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
mesh=bpy.data.meshes.new('CrystalFacets'); mesh.from_pydata(verts,[],faces); mesh.update()
uv=mesh.uv_layers.new(name='CrystalUV')
for loop in mesh.loops:
    v=mesh.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(v.x+.5,v.y+.5)
obj=bpy.data.objects.new('CrystalBlock',mesh); bpy.context.collection.objects.link(obj)
bpy.context.view_layer.objects.active=obj; obj.select_set(True)
mat=bpy.data.materials.new('Crystal'); mat.diffuse_color=(.15,.75,.9,1); mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Base Color'].default_value=(.08,.65,.85,1); bsdf.inputs['Metallic'].default_value=.25; bsdf.inputs['Roughness'].default_value=.18
obj.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Documentation','CrystalBlock.blend'))
# OBJ is exported in game coordinates; Unity faces the negative Z side.
bpy.ops.wm.obj_export(filepath=os.path.join(root,'Assets','Resources','Art','CrystalBlock.obj'),export_selected_objects=True,forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=False)
