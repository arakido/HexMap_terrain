using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCellPriorityQueue {

    private List<HexCell> list = new List<HexCell>() ;
    private int minimum = int.MaxValue ;

    public int Count { get { return count; } }
    private int count ;

    public void Enqueue( HexCell cell ) {
        count++ ;
        int priorty = cell.SearchPriority ;
        if ( priorty < minimum ) minimum = priorty ;
        while ( priorty >= list.Count ) {
            list.Add( null ) ;
        }
        cell.NextWithSamePriority = list[ priorty ] ;
        list[ priorty ] = cell ;
    }

    public HexCell Dequeue() {
        count-- ;
        for ( ; minimum < list.Count ; minimum++ ) {
            HexCell cell = list[minimum] ;
            if ( cell != null ) {
                list[ minimum ] = cell.NextWithSamePriority ;
                return cell ;
            }
        }
        return null ;
    }

    public void Change( HexCell cell , int oldPriority ) {
        HexCell current = list[ oldPriority ] ;
        HexCell next = current.NextWithSamePriority ;
        if ( current == cell ) list[ oldPriority ] = next ;
        else {
            while ( next != cell ) {
                current = next ;
                next = current.NextWithSamePriority ;
            }
            current.NextWithSamePriority = cell.NextWithSamePriority ;
        }
        Enqueue( cell );
        count--; //Enqueue函数中count增加的， 但实际上是没有增加的，只是把cell的优先级改变了而已
    }

    public bool Conten( HexCell cell ) {
        return list.Contains( cell ) ;
    }

    public void Clear() {
        list.Clear() ;
        count = 0 ;
        minimum = int.MaxValue;
    }

}
