# 3D models

The models in this folder come from packs released under CC0 (public domain):

- KayKit Character Pack: Adventurers 1.0, by Kay Lousberg (www.kaylousberg.com)
- KayKit Character Pack: Skeletons 1.0, by Kay Lousberg
- KayKit Dungeon Remastered 1.0, by Kay Lousberg
- Ultimate Monsters, by Quaternius (quaternius.com)

They were trimmed and compressed with `tools/models3d/build-models.mjs`:

```
cd tools/models3d
npm install
node build-models.mjs <folder with the three KayKit repositories> ../../web/src/assets/models <Ultimate Monsters folder>
```
