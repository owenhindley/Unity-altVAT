# Unity-altVAT
Unity-specific alternative system for the VAT (Vertex Animation Textures) technique from Houdini

Background:
I wanted a simpler, more straightforward approach to performing vertex-level displacement on meshes in Unity, based on animated simulations / deformations in Houdini. SideFX have released a tool to do exactly this (Labs Vertex Animation Textures), which has a ton of useful features (for example, the same system handles rigid-body/per-piece animation too).. *but* I was having trouble with getting it to work correctly in my specific usecase (animated VR film for Oculus Quest - something to do with ETC texture compression I suspect).


Advantages of this system:
- Simpler - moves the texture creation logic over to a Unity editor script, for you to extend / modify as you need.
- Animation data comes out of Houdini as a simple CSV list of positions, normals, and UVs, which is then fed into an Editor Script in Unity
- Uses 3D textures (with per-frame data stored in the 'depth' / Z dimension), so conceptually easier to understand / debug
- Simpler shader code (written in ShaderLab, rather than ShaderGraph), so hopefully should provide better performance

(IMPORTANT) Disadvantages of this system:
- Slower - it takes much longer to export everything from Houdini and re-import into Unity.
- Less efficient memory usage - unless you're lucky to have a vertex count that has an integer sqaure root, you're always going to have some wasted texture memory due to padding.
- Compatibility - this is tested and works on Quest 2, but not all platforms may support 3D textures.
- Extensibility - because this is written in Shaderlab code, you'll need to grab the vertex shader from the included shaders and integrate them into a more complex surface shader.

### Instructions

1) In Houdini, install `sop_owen_animsurface_export.1.0` HDA via Asset Manager
2) The HDA automatically ensures triangle topology, adds normals and UVs if they don't exist already.
3) Select an output directory for the CSV file, and a frame start/end, and hit export. This will take a minute or two.
*N.B. These CSV files can get large - e.g. for 3k verts over 800 frames, it comes to around 400MB*
4) In Unity, install the package via Package Manager ('Load from disk')
5) Go to Window -> AltVAT Create Mesh+Textures From CSV
6) Select the CSV file (assuming it's in your project)
7) Texture size is the X/Y dimension of the 3D texture. This needs to 'fit' the number of vertices you have, e.g. if you have 3k vertices, you'll need a size of minimum 55 (55 x 55 = 3025). 

You *may* want to pad this out to a power of 2, depending on your platform, so e.g. 64x64.

8) Texture depth is the number of frames. If you want to do a quick test before importing the whole animation, you can do e.g. 32 frames and it'll just truncate the rest (after giving you a warning in the console), but otherwise this needs to be minimum the number of frames in the animation. Again, you *may* want to pad this out to a power of 2.
9) Hit 'Create Mesh And Textures'. This will again take a while, but if it works, you'll have in the same folder as the CSV file:
- a Prefab containing a Mesh Filter + Renderer, setup with the correct material, shader, bounds data etc.
- a Positions 3D texture
- a Normals 3D texture
- the mesh (created from the first frame of the animation)
- a Material, with the correct Min / Max bounds set for positions & normals respectively. Normally you'll never want to change these bounds, unless you want to exaggerate / control your animation.

10) That's it! Import the prefab into your scene, and move the _NormalisedFrame slider to scroll through the animation.

Hope this is of use to someone!



