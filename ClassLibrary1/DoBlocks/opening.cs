using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace DoBlocks
{
    public class Commands : Window
    {
        private static int _id;
        private int _rDeep;
        private int _rWidth;
        private int _rDistance;
        private int _rHeight;
        private int _rLevels;
        private int _rRacks;
        private BoxCollection _boxs;


        [CommandMethod("DoBlocks")]
        public void Launch()
        {
            var w = new Gui();
            w.Show();
        }

        public void DeSerializeFile(string path)
        {
            try
            {
            var serializer = new XmlSerializer(typeof(BoxCollection));
            var reader = new StreamReader(path);
            _boxs = (BoxCollection)serializer.Deserialize(reader);
            reader.Close();
            GetDocument(_boxs);
            GetDocument2(_boxs);
            }
            catch (Exception)
            {
                MessageBox.Show("File not found.");
            }
        }

        public string GetExtension(string fullName)
        {
            return new FileInfo(fullName).Extension;
        }
        //
        // Récupération des informations sur les blocks dans le fichier excel spécifié
        //
        public void GetInfoExcel(string path)
        {
            string ext = GetExtension(path);
            string filePath = path;
            string connectionString;
            if (ext.Equals(".xlsx"))
                connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=\"Excel 12.0;HDR=YES\";";
            else
                connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=\"Excel 8.0\";";
            var connection = new OleDbConnection(connectionString);
            const string cmdText = "SELECT * FROM [Feuil1$]";
            var command = new OleDbCommand(cmdText, connection);
            command.Connection.Open();
            var reader = command.ExecuteReader();

            // On vérifie qu'il y a bien des lignes dans le fichier.
            if (reader != null && reader.HasRows)
            {
                var bc = new BoxCollection { ArrayBox = new List<Box>() };
                while (reader.Read())
                {
                    // On ajoute à la liste tous les blocs trouvés dans le fichier.
                    bc.ArrayBox.Add(SerializeExcel(reader).Clone() as Box);
                }
                // Ecriture du fichier XML avec les informations récoltées.
                var serializer = new XmlSerializer(typeof(BoxCollection));
                TextWriter textWriter = new StreamWriter(@"C:\Box.xml");
                serializer.Serialize(textWriter, bc);
                textWriter.Close();
            }
        }

        //
        // Récupération des informations concernant les racks dans la GUI
        //
        public void GetInfoRacks(int height, int distance, int width, int levels, int racks, int deep)
        {
            _rHeight = height;
            _rDistance = distance;
            _rDeep = deep;
            _rLevels = levels;
            _rRacks = racks;
            _rWidth = width;
        }

        private Box SerializeExcel(OleDbDataReader reader)
        {
            var b = new Box
                        {
                            BlckName = reader[0].ToString(),
                            Type = Convert.ToInt32(reader[1].ToString()),
                            Ref = Convert.ToInt32(reader[2].ToString()),
                            Rank = Convert.ToInt32(reader[3].ToString()),
                            Line = Convert.ToInt32(reader[4].ToString()),
                            Pos = Convert.ToInt32(reader[5].ToString()),
                            Height = Convert.ToInt32(reader[6].ToString()),
                            Width = Convert.ToInt32(reader[7].ToString()),
                            Deep = Convert.ToInt32(reader[8].ToString()),
                            Double = Convert.ToInt32(reader[9].ToString())
                        };
            return b;
        }

        // Récupération du fichier Autocad et insertion des blocs
        private void GetDocument(BoxCollection boxArray)
        {
            for (int x = 0; x < boxArray.ArrayBox.Count; x++)
            {
                try
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    Database db = doc.Database;
                    Editor ed = doc.Editor;
                    Transaction tr = db.TransactionManager.StartTransaction();
                    using (tr)
                    {
                        // Get the block table from the drawing
                        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        string blkName = checkName(boxArray, bt, x, ed);
                        // Create our new block table record...
                        var btr = new BlockTableRecord { Name = blkName };
                        // ... and set its properties
                        // Add the new block to the block table
                        using (Application.DocumentManager.MdiActiveDocument.LockDocument())
                        {
                            bt.UpgradeOpen();
                            Application.UpdateScreen();
                            ObjectId btrId = bt.Add(btr);
                            tr.AddNewlyCreatedDBObject(btr, true);
                            // Add some lines to the block to form a square
                            // (the entities belong directly to the block)
                            DBObjectCollection ents = SquareOfLines(boxArray.ArrayBox[x].Width,
                                                                    boxArray.ArrayBox[x].Height,
                                                                    boxArray.ArrayBox[x].Deep,
                                                                    boxArray.ArrayBox[x].Type);
                            foreach (Entity ent in ents)
                            {
                                btr.AppendEntity(ent);
                                tr.AddNewlyCreatedDBObject(ent, true);
                            }
                            // Add a block reference to the model space
                            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            BlockReference br = boxArray.ArrayBox[x].Double == 1 && (boxArray.ArrayBox[x].Width * 2) <= _rHeight
                                                    ? new BlockReference(new Point3d(boxArray.ArrayBox[x].Pos * _rWidth,
                                                                                     boxArray.ArrayBox[x].Rank * _rDistance +
                                                                                     boxArray.ArrayBox[x].Width,
                                                                                     boxArray.ArrayBox[x].Line * _rHeight + (_rHeight / 10)),
                                                                         btrId)
                                                    : new BlockReference(new Point3d(boxArray.ArrayBox[x].Pos * _rWidth,
                                                                                     boxArray.ArrayBox[x].Rank * _rDistance,
                                                                                     boxArray.ArrayBox[x].Line * _rHeight + (_rHeight / 10)),
                                                                         btrId);
                            ms.AppendEntity(br);
                            tr.AddNewlyCreatedDBObject(br, true);
                            // Commit the transaction
                            tr.Commit();
                            // Report what we've done
                            ed.WriteMessage("\nCreated block named \"{0}\" containing {1} entities.", blkName,
                                            ents.Count);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("There is a problem in block creation, here is the error : " + e);
                }
            }
        }

        private void GetDocument2(BoxCollection boxArray)
        {
            for (int y = 0; y <= _rLevels; y++)
            {
                for (int x = 0; x < _rRacks; x++)
                {
                    try
                    {
                        Document doc = Application.DocumentManager.MdiActiveDocument;
                        Database db = doc.Database;
                        Editor ed = doc.Editor;
                        Transaction tr = db.TransactionManager.StartTransaction();
                        using (tr)
                        {
                            // Get the block table from the drawing
                            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            string blkName = checkName(boxArray, bt, x, ed);
                            // Create our new block table record...
                            var btr = new BlockTableRecord { Name = blkName };
                            // ... and set its properties
                            // Add the new block to the block table
                            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
                            {
                                bt.UpgradeOpen();
                                Application.UpdateScreen();
                                ObjectId btrId = bt.Add(btr);
                                tr.AddNewlyCreatedDBObject(btr, true);
                                // Add some lines to the block to form a square
                                // (the entities belong directly to the block)
                                DBObjectCollection ents = RacksOfLines(_rHeight, _rDeep);
                                foreach (Entity ent in ents)
                                {
                                    btr.AppendEntity(ent);
                                    tr.AddNewlyCreatedDBObject(ent, true);
                                }
                                // Add a block reference to the model space
                                var ms =
                                    (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                                var br = new BlockReference(new Point3d(0, x * _rDistance, y * _rHeight), btrId);
                                ms.AppendEntity(br);
                                tr.AddNewlyCreatedDBObject(br, true);
                                // Commit the transaction
                                tr.Commit();
                                // Report what we've done
                                ed.WriteMessage("\nCreated block named \"{0}\" containing {1} entities.", blkName,
                                                ents.Count);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("There is a problem in block creation, here is the error : " + e);
                    }
                }
            }
        }

        //
        // Vérification de la non-utilisation du nom des blocks
        //
        private string checkName(BoxCollection boxArray, BlockTable bt, int x, Editor ed)
        {
            string blkName = "";
            do
                try
                {
                    blkName = boxArray.ArrayBox[x].BlckName + _id.ToString(CultureInfo.InvariantCulture);
                    while (bt.Has(blkName))
                    {
                        _id++;
                        blkName = boxArray.ArrayBox[x].BlckName + _id.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    ed.WriteMessage("\nInvalid block name.");
                } while (blkName == "");
            return blkName;
        }

        //
        // Fonction de tracé des lignes entre les points pour afficher les blocks
        //

        private DBObjectCollection RacksOfLines(int height, int deep)
        {
            // A function to generate a set of entities for our block
            var ents = new DBObjectCollection();
            Point3d[] pts = {
                                    new Point3d(0, 0, 0),
                                    new Point3d(0, height, 0),
                                    new Point3d(deep, height, 0),
                                    new Point3d(deep, 0, 0)
                            };
            // first square
            const byte red = 255;
            const byte green = 255;
            const byte blue = 255;
            for (int i = 0; i <= 3; i++)
            {
                int j = (i == 3 ? 0 : i + 1);
                var ln = new Line(pts[i], pts[j]) { Color = Color.FromRgb(red, green, blue) };
                ents.Add(ln);
            }
            return ents;
        }

        private DBObjectCollection SquareOfLines(double height, double width, double deep, int type)
        {
            // A function to generate a set of entities for our block
            var ents = new DBObjectCollection();
            Point3d[] pts = {
                                    new Point3d(0, 0, 0),
                                    new Point3d(0, 0, width),
                                    new Point3d(height, 0, width),
                                    new Point3d(height, 0, 0),
                                    new Point3d(0, deep, 0),
                                    new Point3d(0, deep, width),
                                    new Point3d(height, deep, width),
                                    new Point3d(height, deep, 0)
                                };
            // first square
            byte red = 0, green, blue = 0;
            switch (type)
            {
                case 1:
                    red = 255; green = 150; break;
                case 2:
                    green = 255; break;
                case 3:
                    green = 255; blue = 255; break;
                default:
                    red = 255; green = 255; blue = 255;
                    break;
            }
            Line ln;
            for (int i = 0; i <= 3; i++)
            {
                int j = (i == 3 ? 0 : i + 1);
                ln = new Line(pts[i], pts[j]) { Color = Color.FromRgb(red, green, blue) };
                ents.Add(ln);
            }
            // second square
            for (int i = 4; i <= 7; i++)
            {
                int j = (i == 7 ? 4 : i + 1);
                ln = new Line(pts[i], pts[j]) { Color = Color.FromRgb(red, green, blue) };
                ents.Add(ln);
            }
            // Complete cube
            for (int i = 0; i <= 3; i++)
            {
                int j = i + 4;
                ln = new Line(pts[i], pts[j]) { Color = Color.FromRgb(red, green, blue) };
                ents.Add(ln);
            }
            return ents;
        }

        public void LookForPos(int rack, int level, int pos)
        {
            try
            {
                int x = 0;
                foreach (var b in _boxs.ArrayBox.Where(b => b.Rank == rack && b.Line == level && b.Pos == pos))
                {
                    MessageBox.Show("Box properties : \n\tName : " + b.BlckName + "\n\tRack : " + b.Rank +
                                    "\n\tLevel : " + b.Line + "\n\tPosition : " + b.Pos + "\n\n\tWidth : " +
                                    b.Width + "\n\tHeight : " + b.Height + "\n\tDeep : " + b.Deep);
                    x++;
                }
                if (x == 0)
                    MessageBox.Show("No items found");
            }
            catch (Exception)
            {
                MessageBox.Show("Erreur dans le parcours par position");
            }
        }

        public void LookForName(string name)
        {
            try
            {
                int x = 0;
                foreach (var b in _boxs.ArrayBox.Where(b => b.BlckName == name))
                {
                    MessageBox.Show("Box properties : \n\t Name : " + b.BlckName + "\n\tRack : " + b.Rank +
                                    "\n\tLevel : " + b.Line + "\n\t" + "\n\tPosition : " + b.Pos + "\n\tWidth : " +
                                    b.Width + "\n\tHeight : " + b.Height + "\n\tDeep : " + b.Deep);
                    x++;
                }
                if (x == 0)
                    MessageBox.Show("No items found");
            }
            catch (Exception)
            {
                MessageBox.Show("Erreur dans le parcours par nom");
            }
        }
    }
}
