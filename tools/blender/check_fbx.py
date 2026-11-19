"""Confere um FBX binario sem Blender nem Unity: tamanho da malha (como o Unity a ve) e triangulos.
Uso:  python3 tools/blender/check_fbx.py <arquivo.fbx> <x> <y> <z>
      python3 tools/blender/check_fbx.py Assets/Game/Content/Models/Piece.fbx 0.9 0.9 0.1
"""
import struct, sys, zlib


def read_nodes(data):
    version = struct.unpack("<I", data[23:27])[0]
    wide = version >= 7500
    head = 25 if wide else 13

    def node(pos):
        fmt = "<QQQB" if wide else "<IIIB"
        end, n_props, _, name_len = struct.unpack(fmt, data[pos:pos + head])
        if end == 0:
            return None, pos + head
        name = data[pos + head:pos + head + name_len].decode()
        p = pos + head + name_len
        props = []
        for _ in range(n_props):
            t = chr(data[p]); p += 1
            if t in "fdilb":                       # arrays
                count, enc, size = struct.unpack("<III", data[p:p + 12]); p += 12
                raw = data[p:p + size]; p += size
                if enc: raw = zlib.decompress(raw)
                props.append(struct.unpack("<%d%s" % (count, t.replace("l", "q")), raw))
            elif t in "SR":                        # texto ou bytes
                size = struct.unpack("<I", data[p:p + 4])[0]; p += 4 + size
            else:                                  # escalares
                fm = {"Y": "<h", "C": "<?", "I": "<i", "F": "<f", "D": "<d", "L": "<q"}[t]
                p += struct.calcsize(fm)
        kids = []
        while p < end:
            kid, p = node(p)
            if kid is None: break
            kids.append(kid)
        return (name, props, kids), end

    pos, nodes = 27, []
    while pos < len(data) - 200:
        n, pos = node(pos)
        if n is None: break
        nodes.append(n)
    return nodes


def find(nodes, name):
    for n in nodes:
        if n[0] == name: yield n
        yield from find(n[2], name)


nodes = read_nodes(open(sys.argv[1], "rb").read())
v = next(find(nodes, "Vertices"))[1][0]
idx = next(find(nodes, "PolygonVertexIndex"))[1][0]
xs, ys, zs = v[0::3], v[1::3], v[2::3]
size = (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))
# triangulos: cada poligono termina em um indice negativo
polys, count = [], 0
for i in idx:
    count += 1
    if i < 0: polys.append(count); count = 0
tris = sum(c - 2 for c in polys)
want = [float(a) for a in sys.argv[2:5]] if len(sys.argv) >= 5 else [0.9, 0.9, 0.1]
print("tamanho (x, y, z) = (%.3f, %.3f, %.3f)" % size)
print("triangulos =", tris)
ok = all(abs(size[i] - want[i]) < .005 for i in range(3)) and tris <= 300
print("OK: %s x %s x %s, ate 300 triangulos" % tuple(want) if ok else "FALHOU: esperado %s x %s x %s" % tuple(want))
sys.exit(0 if ok else 1)