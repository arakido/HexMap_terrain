using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListPool<T> {
    private static Stack<List<T>> stack = new Stack<List<T>>() ;

    public static List<T> Get() {
        if ( stack.Count <= 0 ) return new List<T>() ;
        return stack.Pop() ;
    }

    public static void Add( List<T> list ) {
        list.Clear() ;
        stack.Push( list ) ;
    }

}
