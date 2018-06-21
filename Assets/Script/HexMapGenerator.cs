using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

    public HexGrid hexGrid ;
    public bool useFixedSeed ;
    public int randmSeed ;
    [Range( 0f , 0.5f )] public float jitterProbability = 0.25f ;
    [Range( 20 , 200 )] public int chunkSizeMin = 30 ;
    [Range( 20 , 200 )] public int chunkSizeMax = 100 ;
    [Range( 0f , 1f )] public float highRiseProbability = 0.25f ;
    [Range( 0f , 0.4f )] public float sinkProbability = 0.2f ;
    [Range( 5 , 95 )] public int landPercentage = 50 ;
    [Range( 1 , 5 )] public int waterLevel = 3 ;
    [Range( -4 , 0 )] public int elevationMinimum = -2 ;
    [Range( 6 , 10 )] public int elevationMaxmum = 8 ;
    [Range( 0 , 10 )] public int mapBorderX = 5 ;
    [Range( 0 , 10 )] public int mapBorderZ = 5 ;
    [Range( 0 , 10 )] public int regionBorder = 5 ;
    [Range( 1 , 4 )] public int regionCount = 1 ;
    [Range( 0 , 100 )] public int erosionPercentage = 50 ;

    private int cellCount ;
    private HexCellPriorityQueue searchFrontier ;
    private int searchFrontierPhase ;

    private struct MapRegion {
        public int xMin, xMax, zMin, zMax;

    }

    private List<MapRegion> regionList ;

    public void GeneratorMap( int x , int z ) {
        Random.State originalRandomState = Random.state ;
        if(!useFixedSeed) {
            randmSeed = Random.Range(0, int.MaxValue);
            randmSeed ^= (int)System.DateTime.Now.Ticks;
            randmSeed ^= (int)Time.unscaledTime;
            randmSeed &= int.MaxValue;
            Random.InitState(randmSeed);
        }

        cellCount = x * z ;
        hexGrid.CreateMap( x , z ) ;
        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        for (int i = 0; i < cellCount; i++) {
            hexGrid.GetCell(i).WaterLevel = waterLevel;
        }
        CreateRegions( x , z ) ;
        CreateLand() ;
        ErodeLand() ;
        SetTerrainType() ;
        for (int i = 0; i < cellCount; i++) {
            hexGrid.GetCell( i ).SearchPhase = 0;
        }

        Random.state = originalRandomState ;
    }

    private void CreateRegions(int x,int z) {
        if ( regionList == null ) regionList = new List<MapRegion>() ;
        else  regionList.Clear();

        switch ( regionCount ) {
            case 2:
                MapRegion mapRegion;
                if ( Random.value < 0.5f ) {
                    mapRegion.xMin = mapBorderX ;
                    mapRegion.xMax = x / 2 - regionBorder ;
                    mapRegion.zMin = mapBorderZ ;
                    mapRegion.zMax = z - mapBorderZ ;
                    regionList.Add( mapRegion ) ;

                    mapRegion.xMin = x / 2 + regionBorder ;
                    mapRegion.xMax = x - mapBorderX ;
                    regionList.Add( mapRegion ) ;
                }
                else {
                    mapRegion.xMin = mapBorderX ;
                    mapRegion.xMax = x - mapBorderX ;
                    mapRegion.zMin = mapBorderZ ;
                    mapRegion.zMax = z / 2 - regionBorder ;
                    regionList.Add( mapRegion ) ;

                    mapRegion.zMin = z / 2 + regionBorder ;
                    mapRegion.zMax = z - mapBorderZ ;
                    regionList.Add( mapRegion ) ;
                }
                break ;
            case 3:
                mapRegion.xMin = mapBorderX;
                mapRegion.xMax = x / 3 - regionBorder;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);

                mapRegion.xMin = x / 3 + regionBorder;
                mapRegion.xMax = x * 2 / 3 - regionBorder;
                regionList.Add(mapRegion);

                mapRegion.xMin = x * 2 / 3 + regionBorder;
                mapRegion.xMax = x - mapBorderX;
                regionList.Add(mapRegion);
                break ;
            case 4:
                mapRegion.xMin = mapBorderX;
                mapRegion.xMax = x / 2 - regionBorder;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z / 2 - regionBorder;
                regionList.Add(mapRegion);

                mapRegion.xMin = x / 2 + regionBorder;
                mapRegion.xMax = x - mapBorderX;
                regionList.Add(mapRegion);

                mapRegion.zMin = z / 2 + regionBorder;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);

                mapRegion.xMin = mapBorderX;
                mapRegion.xMax = x / 2 - regionBorder;
                regionList.Add(mapRegion);
                break ;
            default:
                mapRegion.xMin = mapBorderX;
                mapRegion.xMax = x - mapBorderX;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);
                break ;
        }
        
        
    }

    private void CreateLand() {
        int landBudget = Mathf.RoundToInt( cellCount * landPercentage * 0.01f ) ;
        for ( int guard = 0 ; guard < 10000 ; guard++ ) {
            bool sink = Random.value < sinkProbability ;
            for ( int i = 0 ; i < regionList.Count ; i++ ) {
                MapRegion region = regionList[ i ] ;
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
                if ( sink ) landBudget = SinkTerrain( chunkSize , landBudget , region ) ;
                else {
                    landBudget = RaiseTerrain( chunkSize , landBudget , region ) ;
                    if ( landBudget <= 0 ) return ;
                }
            }
        }
        if ( landBudget > 0 ) {
            Debug.LogError( " Faild to use Up " + landBudget+" land buget");
        }
    }

    private int RaiseTerrain( int chunkSize ,int budget ,MapRegion region) {
        searchFrontierPhase += 1 ;
        HexCell firestCell = GetRandomCell(region) ;
        firestCell.SearchPhase = searchFrontierPhase ;
        firestCell.Distance = 0 ;
        firestCell.SearchHeuristic = 0 ;
        searchFrontier.Enqueue( firestCell );
        HexCoordinates center = firestCell.coordinates ;

        int rise = Random.value < highRiseProbability ? 2 : 1 ;
        int size = 0 ;
        while ( size<chunkSize && searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            int originalElevation = current.Elevation ;
            int newElevation = originalElevation + rise ;
            if ( newElevation > elevationMaxmum ) continue ;
            current.Elevation = newElevation ;
            if ( originalElevation < waterLevel && newElevation >= waterLevel && --budget == 0 ) break ;
            
            size += 1 ;

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;
                if ( neighbor && neighbor.SearchPhase < searchFrontierPhase ) {
                    neighbor.SearchPhase = searchFrontierPhase ;
                    neighbor.Distance = neighbor.coordinates.DistancesTo( center ) ;
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0 ;
                    searchFrontier.Enqueue( neighbor );
                }
            }
        }

        searchFrontier.Clear();

        return budget ;
    }

    private int SinkTerrain( int chunkSize , int budget, MapRegion region) {
        searchFrontierPhase += 1 ;
        HexCell firestCell = GetRandomCell(region) ;
        firestCell.SearchPhase = searchFrontierPhase ;
        firestCell.Distance = 0 ;
        firestCell.SearchHeuristic = 0 ;
        searchFrontier.Enqueue( firestCell ) ;
        HexCoordinates center = firestCell.coordinates ;

        int sink = Random.value < highRiseProbability ? 2 : 1 ;
        int size = 0 ;
        while ( size < chunkSize && searchFrontier.Count > 0 ) {
            HexCell current = searchFrontier.Dequeue() ;
            int originalElevation = current.Elevation ;
            int newElevation = originalElevation - sink;
            if ( newElevation < elevationMinimum ) continue ;
            current.Elevation = newElevation;
            ;
            if ( originalElevation >= waterLevel && newElevation < waterLevel ) budget += 1 ;

            size += 1 ;

            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = current.GetNeighbor( d ) ;
                if ( neighbor && neighbor.SearchPhase < searchFrontierPhase ) {
                    neighbor.SearchPhase = searchFrontierPhase ;
                    neighbor.Distance = neighbor.coordinates.DistancesTo( center ) ;
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0 ;
                    searchFrontier.Enqueue( neighbor ) ;
                }
            }
        }

        searchFrontier.Clear() ;

        return budget ;
    }

    private HexCell GetRandomCell(MapRegion region) {
        return hexGrid.GetCell( Random.Range( region.xMin , region.xMax ) ,
                                Random.Range( region.zMin , region.zMax ) ) ;
    }
    
    //风化地形
    private void ErodeLand() {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get() ;
        for ( int i = 0 ; i < cellCount ; i++ ) {
            HexCell cell = hexGrid.GetCell( i ) ;
            if ( IsErodible( cell ) ) erodibleCells.Add( cell ) ;
        }

        int targetErodibleCount = (int) (erodibleCells.Count * (100 - erosionPercentage) * 0.01f) ;

        while ( erodibleCells.Count > targetErodibleCount ) {
            int index = Random.Range( 0 , erodibleCells.Count ) ;
            HexCell cell = erodibleCells[ index ] ;
            HexCell targetCell = GetErosionTarget( cell ) ;
            cell.Elevation -= 1 ;
            targetCell.Elevation += 1 ;

            if ( !IsErodible( cell ) ) {
                erodibleCells[ index ] = erodibleCells[ erodibleCells.Count - 1 ] ;
                erodibleCells.RemoveAt( erodibleCells.Count - 1 ) ;
            }
            for (HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = cell.GetNeighbor( d ) ;
                if ( neighbor && neighbor.Elevation == cell.Elevation +2 && !erodibleCells.Contains( neighbor ) ) {
                    erodibleCells.Add( neighbor );
                }
            }

            if ( IsErodible( targetCell ) && !erodibleCells.Contains( targetCell ) ) {
                erodibleCells.Add( targetCell );
            }

            for (HexDirectionEnum d = 0; d < HexDirectionEnum.Length; d++) {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if ( neighbor && neighbor != cell && neighbor.Elevation == (targetCell.Elevation + 1) && !IsErodible( neighbor ) ) {
                    erodibleCells.Remove( neighbor ) ;
                }
            }
        }

        ListPool<HexCell>.Add( erodibleCells );
    }

    private bool IsErodible( HexCell cell ) {
        int erodibleElevation = cell.Elevation - 2 ;
        for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
            HexCell neighbor = cell.GetNeighbor( d ) ;
            if ( neighbor && neighbor.Elevation <= erodibleElevation ) return true ;
        }
        return false ;
    }

    private HexCell GetErosionTarget( HexCell cell ) {
        List<HexCell> candidates = ListPool<HexCell>.Get() ;
        int erodibleElevation = cell.Elevation - 2 ;
        for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
            HexCell neighbor = cell.GetNeighbor( d ) ;
            if ( neighbor && neighbor.Elevation <= erodibleElevation ) {
                candidates.Add( neighbor );
            }
        }
        HexCell target = candidates[ Random.Range( 0 , candidates.Count ) ] ;
        ListPool<HexCell>.Add( candidates );
        return target ;
    }


    private void SetTerrainType() {
        for ( int i = 0 ; i < cellCount ; i++ ) {
            HexCell cell = hexGrid.GetCell( i ) ;
            if ( !cell.IsUnderWater ) {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }
}
