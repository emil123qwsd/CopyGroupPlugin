using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try 
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;//получили доступ к документу
                Document doc = uiDoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter();

                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");//выбрали мышкой на экране
                Element element = doc.GetElement(reference);
                Group group = element as Group; //преобразовали выбранные объекты в группу

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                XYZ groupCenter = GetElementCenter(group);

                Room room = GetRoomByPoint(doc, groupCenter);

                Room room2 = GetRoomByPoint(doc, point);

                XYZ roomCenter = GetElementCenter(room);

                XYZ roomCenter2 = GetElementCenter(room2);

                XYZ offset = groupCenter - roomCenter;

                XYZ point2 = roomCenter2 + offset;

                Transaction transaction = new Transaction(doc);//трансакция=изменение модели чертежа, doc ссылка на документ в котором выполняется изменение
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(point2, group.GroupType);
                transaction.Commit();

                return Result.Succeeded;
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element) 
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }
        public Room GetRoomByPoint(Document doc, XYZ point) 
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room!=null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
