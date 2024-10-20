using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.FreeDesktop.DBusIme;
using Avalonia.Input;
using Digger.Architecture;

namespace Digger;

class Terrain : ICreature
{
    public string GetImageFileName()
    {
        return "Terrain.png";
    }
    public int GetDrawingPriority()
    {
        return 20;
    }
    public CreatureCommand Act(int x, int y)
    {
        var command = new CreatureCommand();
        return command;
    }
    public bool DeadInConflict(ICreature conflictedObject)
    {
        return conflictedObject is Player;
    }
}
class Player : ICreature
{
    public int PlayerX;
    public int PlayerY;
    public string GetImageFileName()
    {
        return "Digger.png";
    }
    public int GetDrawingPriority()
    {
        return 14;
    }
    public CreatureCommand Act(int x, int y)
    {
        PlayerX = x;
        PlayerY = y;
        var newPosition = new CreatureCommand();
        if (Game.MapWidth<x&&Game.MapHeight<y)
            return newPosition;
        if (Game.KeyPressed == Key.Up&& y>0&& !IsWall(x, y, Game.KeyPressed))
            newPosition.DeltaY = -1;
        if(Game.KeyPressed == Key.Down&&y<Game.MapHeight-1 && !IsWall(x, y, Game.KeyPressed))
            newPosition.DeltaY = 1;
        if(Game.KeyPressed == Key.Left && x>0 && !IsWall(x, y, Game.KeyPressed))
            newPosition.DeltaX = -1;
        if (Game.KeyPressed == Key.Right && x<Game.MapWidth-1 && !IsWall(x, y, Game.KeyPressed))
            newPosition.DeltaX = 1;
        newPosition.TransformTo=this;
        return newPosition;
    }
    public bool IsWall(int x,int y,Key key)
    {
        if (key == Key.Up)
            return Game.Map[x, y - 1]is Sack;
        if (key == Key.Down)
            return Game.Map[x, y + 1] is Sack;
        if (key == Key.Left)
            return Game.Map[x-1, y] is Sack;
        if (key == Key.Right)
            return Game.Map[x+1,y] is Sack;
        return false;
    }
    public bool DeadInConflict(ICreature conflictedObject)
    {
        if (conflictedObject is Sack)
            return ((Sack)conflictedObject).state;
        if (conflictedObject is Monster)
            return true;
        return false;
    }
}
class Sack : ICreature
{
    public bool state;
    public int countFalling=0;
    public string GetImageFileName()
    {
        return "Sack.png";
    }
    public int GetDrawingPriority()
    {
        return 0;
    }
    public CreatureCommand Act(int x, int y)
    {
        var newPosition = new CreatureCommand();
        IsFall(x,y);
        if (state == true)
        {
            countFalling = countFalling+ 1;
            newPosition.DeltaY = 1;
        }
        if (state == false && countFalling > 1)
        {
            newPosition.TransformTo = new Gold();
            return newPosition;
        }
        if (state == false && countFalling<=1)
            countFalling = 0;
        newPosition.TransformTo = this;
        return newPosition;
    }
    public void IsFall(int x,int y)
    {
        if(y != Game.MapHeight - 1)
        {
            if (Game.Map[x, y + 1] is Player && state == false || Game.Map[x, y + 1] is Monster)
                return;
            state = (Game.Map[x, y + 1] is Player || Game.Map[x, y + 1] is null||Game.Map[x, y + 1] is Monster);
        }
        else state=false;
    }
    public bool DeadInConflict(ICreature conflictedObject)
    {
        if((countFalling>1)&& !(conflictedObject is Player)&&!(conflictedObject is Monster))
            return true;
        return false;
    }
}
class Gold : ICreature
{
    public string GetImageFileName()
    {
        return "Gold.png";
    }
    public int GetDrawingPriority()
    {
        return 12;
    }
    public CreatureCommand Act(int x, int y)
    {
        var command = new CreatureCommand();
        return command;
    }
    public bool DeadInConflict(ICreature conflictedObject)
    {
        if(conflictedObject is Player)
        {
            Game.Scores += 10;
            return true;
        }
        if (conflictedObject is Monster)
            return true;
        return false;
            
    }
}
class Vertex
{
    public int X;
    public int Y;
    public Vertex(int x, int y)
    {
        this.X = x;
        this.Y = y; 
    }
}
class Monster :ICreature
{
    static int idGen = 0;
    int id = 0;
    Player Target;

    public Monster()
    {
        id = idGen;
        idGen++;
    }
    public void FindTarget()
    {
        for(int x=0;x<Game.MapWidth;x++)
            for(int y=0;y<Game.MapHeight; y++)
                if(Game.Map[x,y]is Player)
                {
                    Target = (Player)Game.Map[x,y];
                    return;
                }
    }
    public string GetImageFileName()
    {
        return "Monster.png";
    }
    public int GetDrawingPriority()
    {
        return 0;
    }
    public CreatureCommand Act(int x, int y)
    {
        FindTarget();
        var newPosition = new CreatureCommand();
        if (Target != null)
        {
            var moveTo = Dijkstra(x,y);
            if (x < moveTo.X && !(Game.Map[moveTo.X, moveTo.Y] is Monster))
                newPosition.DeltaX = 1;
            if (x > moveTo.X && !(Game.Map[moveTo.X, moveTo.Y] is Monster))
                newPosition.DeltaX = -1;
            if (y < moveTo.Y && !(Game.Map[moveTo.X, moveTo.Y] is Monster))
                newPosition.DeltaY = 1;
            if (y > moveTo.Y && !(Game.Map[moveTo.X, moveTo.Y] is Monster))
                newPosition.DeltaY = -1;
            newPosition.TransformTo = this;
            return newPosition;
        }
        return newPosition;

            
    }
    public Vertex Dijkstra(int x,int y)
    {
        var point = new Vertex(x, y);
        var root= new PriorityQueue<Vertex, int>();
        var vertexs = new int[Game.MapWidth,Game.MapHeight];
        for(int i=0; i<vertexs.GetLength(0);i++)
            for(int j=0; j<vertexs.GetLength(1);j++)
                vertexs[i,j] = int.MaxValue;
        var visitMap=new bool[Game.MapWidth,Game.MapHeight];
        root.Enqueue(point, 0);
        vertexs[x,y]=0;
        visitMap[x, y] = true;
        var player=new Vertex(Target.PlayerX,Target.PlayerY);
        while (root.Count!=0)
        {
            var minElement=root.Dequeue();
            if (minElement == player)
                break;
            var vertex=Move(minElement);
            foreach(var element in vertex)
            {
                vertexs[element.X, element.Y] = Math.Min(vertexs[minElement.X, minElement.Y]+1, vertexs[element.X, element.Y]);
                if (visitMap[element.X, element.Y] == false)
                {
                    root.Enqueue(element, vertexs[minElement.X, minElement.Y] + 1);
                    visitMap[element.X, element.Y] = true;
                }
            }        
        }
        var coord = new Vertex(player.X,player.Y);
        var path = new List<Vertex>();
        path.Add(new Vertex(coord.X, coord.Y));
        while (coord.X!=point.X||coord.Y!=point.Y)
        {
            var newCoord=new Vertex(coord.X,coord.Y);
            if (coord.X-1>=0&&(vertexs[coord.X-1,coord.Y]<vertexs[coord.X, coord.Y])&& vertexs[coord.X - 1,coord.Y]!=-1)
            {
                coord.X -= 1;
                newCoord.X = coord.X;
                path.Add(newCoord);
                continue;
            }
            if ((coord.X + 1 <Game.MapWidth) && (vertexs[coord.X + 1, coord.Y] < vertexs[coord.X, coord.Y])&& vertexs[coord.X + 1, coord.Y]!=-1)
            {
                coord.X += 1;
                newCoord.X = coord.X;
                path.Add(newCoord);
                continue;
            }
            if (coord.Y-1>=0 &&(vertexs[coord.X, coord.Y-1] < vertexs[coord.X, coord.Y])&& vertexs[coord.X, coord.Y - 1]!=-1)
            {
                coord.Y -= 1;
                newCoord.Y = coord.Y;
                path.Add(newCoord);
                continue;
            }
            if ((coord.Y+1<Game.MapHeight)&&(vertexs[coord.X, coord.Y+1] < vertexs[coord.X, coord.Y])&& vertexs[coord.X, coord.Y + 1]!=-1)
            {
                coord.Y += 1;
                newCoord.Y = coord.Y;
                path.Add(newCoord);
                continue ;
            }
            return point;
        }
        if (path.Count < 2)
            return point;
        return path[path.Count - 2];
    }
    public List<Vertex> Move(Vertex point)
    {
        var listMove=new List<Vertex>();
        var left=new Vertex(point.X-1, point.Y);
        var right=new Vertex(point.X+1, point.Y);
        var top=new Vertex(point.X, point.Y-1);
        var down=new Vertex(point.X, point.Y+1);
        if (left.X >= 0&&!(Game.Map[left.X,left.Y]is Sack)&& !(Game.Map[left.X,left.Y] is Terrain))
            listMove.Add(left);
        if (right.X < Game.MapWidth-1&&!(Game.Map[right.X, right.Y] is Sack) && !(Game.Map[right.X, right.Y] is Terrain))
            listMove.Add(right);
        if (top.Y >= 0&&!(Game.Map[top.X, top.Y] is Sack) && !(Game.Map[top.X, top.Y] is Terrain))
            listMove.Add(top);
        if (down.Y < Game.MapHeight-1&&!(Game.Map[down.X, down.Y] is Sack) && !(Game.Map[down.X, down.Y] is Terrain))
            listMove.Add(down);
        return listMove;
    }
    public bool DeadInConflict(ICreature conflictedObject)
    {
        if (conflictedObject is Sack)
            return ((Sack)conflictedObject).state;
        return false;
    }
}
//Напишите здесь классы Player, Terrain и другие.