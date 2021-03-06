﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

    public HexGrid hexGrid ;
    public bool useFixedSeed ;
    public int randmSeed ;
    [Range( 0f , 0.5f )] public float jitterProbability = 0.25f ;//所谓的随机性
    [Range( 20 , 200 )] public int chunkSizeMin = 30 ;  //区域的最小半径
    [Range( 20 , 200 )] public int chunkSizeMax = 100; //区域的最大半径
    [Range( 0f , 1f )] public float highRiseProbability = 0.25f ;   //生成悬崖的概率
    [Range( 0f , 0.4f )] public float sinkProbability = 0.2f ;  //沉入水平面的概率
    [Range( 5 , 95 )] public int landPercentage = 50 ;  //露出水平面的地图的大小
    [Range( 1 , 5 )] public int waterLevel = 3 ;    //水平面的高度
    [Range( -4 , 0 )] public int elevationMinimum = -2 ;    //最小高度
    [Range( 6 , 10 )] public int elevationMaxmum = 8 ;  //最大高度
    [Range( 0 , 10 )] public int mapBorderX = 5 ;   //距x边的距离
    [Range( 0 , 10 )] public int mapBorderZ = 5;   //距z边的距离
    [Range( 0 , 10 )] public int regionBorder = 5 ; //两区域间的距离
    [Range( 1 , 4 )] public int regionCount = 1 ;   //地图生成区域的数量
    [Range( 0 , 100 )] public int erosionPercentage = 50 ;  //地图风化百分比
    [Range( 0f , 1f )] public float startingMoisture = 0.1f ;   //初始湿度
    [Range( 0f , 1f )] public float evaporationFactor = 0.5f ;    //水汽蒸发系数
    [Range( 0f , 1f )] public float precipitationFactor = 0.25f ;   //降水因子
    [Range( 0f , 1f )] public float runoffFactor = 0.25f ;   //水流量因子
    [Range( 0f , 1f )] public float seepageFactor = 0.125f ;    //水分渗透因子
    public HexDirectionEnum windDirection = HexDirectionEnum.TopLeft;   //风向
    [Range( 1f , 10f )] public float windStrength = 4f ;    //风力
    [Range( 0 , 20 )] public float riverPercentage = 10 ;   //河流占比
    [Range( 0f , 1f )] public float extraLakeProbability = 0.25f ;  //湖泊占比
    [Range( 0f , 1f )] public float lowTemperature = 0f ;   //低温
    [Range( 0f , 1f )] public float highTemperature = 1f ;  //高温
    [Range( 0f , 1f )] public float temperatureJitter = 0.1f ;  //温度波动
    public HemisphereMode hemisphere;


    private int cellCount ;
    private int landCells ;
    private HexCellPriorityQueue searchFrontier ;
    private int searchFrontierPhase ;

    private struct MapRegion {
        public int xMin, xMax, zMin, zMax;
    }

    private List<MapRegion> regionList ;

    private struct ClimateData {
        public float clouds ,moisture;
    }

    private List<ClimateData> climateList = new List<ClimateData>() ;
    private List<ClimateData> nextClimateList = new List<ClimateData>() ; 

    public void GeneratorMap( int x , int z ,bool wrapping) {

        Random.State originalRandomState = Random.state ;
        if(!useFixedSeed) {
            randmSeed = Random.Range(0, int.MaxValue);
            randmSeed ^= (int)System.DateTime.Now.Ticks;
            randmSeed ^= (int)Time.unscaledTime;
            randmSeed &= int.MaxValue;
            Random.InitState(randmSeed);
        }

        cellCount = x * z ;
        hexGrid.CreateMap( x , z , wrapping) ;
        if ( searchFrontier == null ) searchFrontier = new HexCellPriorityQueue() ;
        for (int i = 0; i < cellCount; i++) {
            hexGrid.GetCell(i).WaterLevel = waterLevel;
        }
        CreateRegions( x , z ) ;
        CreateLand() ;
        ErodeLand() ;
        CreateClimate();
        CreateRivers() ;
        SetTerrainType() ;
        for (int i = 0; i < cellCount; i++) {
            hexGrid.GetCell( i ).SearchPhase = 0;
        }

        Random.state = originalRandomState ;
    }

    #region 地形
    private void CreateRegions(int x,int z) {
        if ( regionList == null ) regionList = new List<MapRegion>() ;
        else  regionList.Clear();

        int borderX = hexGrid.wrapping ? regionBorder : mapBorderX ;

        MapRegion mapRegion;
        switch ( regionCount ) {
            case 2:
                if ( Random.value < 0.5f ) {
                    mapRegion.xMin = borderX;
                    mapRegion.xMax = x / 2 - regionBorder ;
                    mapRegion.zMin = mapBorderZ ;
                    mapRegion.zMax = z - mapBorderZ ;
                    regionList.Add( mapRegion ) ;

                    mapRegion.xMin = x / 2 + regionBorder ;
                    mapRegion.xMax = x - borderX;
                    regionList.Add( mapRegion ) ;
                }
                else {
                    if (hexGrid.wrapping) borderX = 0;
                    mapRegion.xMin = borderX;
                    mapRegion.xMax = x - borderX;
                    mapRegion.zMin = mapBorderZ ;
                    mapRegion.zMax = z / 2 - regionBorder ;
                    regionList.Add( mapRegion ) ;

                    mapRegion.zMin = z / 2 + regionBorder ;
                    mapRegion.zMax = z - mapBorderZ ;
                    regionList.Add( mapRegion ) ;
                }
                break ;
            case 3:
                mapRegion.xMin = borderX;
                mapRegion.xMax = x / 3 - regionBorder;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);

                mapRegion.xMin = x / 3 + regionBorder;
                mapRegion.xMax = x * 2 / 3 - regionBorder;
                regionList.Add(mapRegion);

                mapRegion.xMin = x * 2 / 3 + regionBorder;
                mapRegion.xMax = x - borderX;
                regionList.Add(mapRegion);
                break ;
            case 4:
                mapRegion.xMin = borderX;
                mapRegion.xMax = x / 2 - regionBorder;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z / 2 - regionBorder;
                regionList.Add(mapRegion);

                mapRegion.xMin = x / 2 + regionBorder;
                mapRegion.xMax = x - borderX;
                regionList.Add(mapRegion);

                mapRegion.zMin = z / 2 + regionBorder;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);

                mapRegion.xMin = borderX;
                mapRegion.xMax = x / 2 - regionBorder;
                regionList.Add(mapRegion);
                break ;
            default:
                if ( hexGrid.wrapping ) borderX = 0 ;
                mapRegion.xMin = borderX;
                mapRegion.xMax = x - borderX;
                mapRegion.zMin = mapBorderZ;
                mapRegion.zMax = z - mapBorderZ;
                regionList.Add(mapRegion);
                break ;
        }
        
        
    }

    private void CreateLand() {
        int landBudget = Mathf.RoundToInt( cellCount * landPercentage * 0.01f ) ;
        landCells = landBudget ;
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
            landCells -= landBudget ;
            Debug.LogWarning( " Faild to use Up " + landBudget+" land buget");
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
        while ( size < chunkSize && searchFrontier.Count > 0 ) {
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
                    neighbor.Distance = neighbor.coordinates.DistanceTo( center ) ;
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
                    neighbor.Distance = neighbor.coordinates.DistanceTo( center ) ;
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

    #endregion

    #region 气候

    private void CreateClimate() {
        climateList.Clear() ;
        nextClimateList.Clear() ;
        ClimateData initialData = new ClimateData() ;
        initialData.moisture = startingMoisture ;
        ClimateData clearData = new ClimateData();
        for ( int i = 0 ; i < cellCount ; i++ ) {
            climateList.Add( initialData ) ;
            nextClimateList.Add( clearData ) ;
        }

        for ( int cycle = 0 ; cycle < 40 ; cycle++ ) {
            for ( int i = 0 ; i < cellCount ; i++ ) {
                EvolveClimate( i ) ;
            }
            List<ClimateData> swap = climateList ;
            climateList = nextClimateList ;
            nextClimateList = swap ;
        }

    }

    private void EvolveClimate( int cellIndex ) {
        HexCell cell = hexGrid.GetCell( cellIndex ) ;
        ClimateData cellClimate = climateList[ cellIndex ] ;
        if ( cell.IsUnderWater ) {
            cellClimate.moisture = 1f ;
            cellClimate.clouds += evaporationFactor ;
        }
        else {
            float evaporation = cellClimate.moisture * evaporationFactor ;
            cellClimate.moisture -= evaporation ;
            cellClimate.clouds += evaporation ;
        }

        float precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.moisture += precipitation ;
        cellClimate.clouds -= precipitation ;

        float cloudMaximum = 1f - cell.ViewElevation / (elevationMaxmum + 1f) ;
        if ( cellClimate.clouds > cloudMaximum ) {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum ;
            cellClimate.clouds = cloudMaximum ;
        }

        HexDirectionEnum mainDispersaDirection = windDirection.Opposite() ;
        float cloudDispersal = cellClimate.clouds * (1 / (5f + windStrength)) ;
        float runoff = cellClimate.moisture * runoffFactor * (1 / 6f) ;
        float seepage = cellClimate.moisture * seepageFactor * (1 / 6f) ;

        for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
            HexCell neighbor = cell.GetNeighbor( d ) ;
            if ( !neighbor ) continue ;

            ClimateData neighborClimate = nextClimateList[ neighbor.Index ] ;
            if ( d == mainDispersaDirection ) neighborClimate.clouds += cloudDispersal * windStrength ;
            else neighborClimate.clouds += cloudDispersal ;

            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation ;
            if ( elevationDelta < 0 ) {
                cellClimate.moisture -= runoff ;
                neighborClimate.moisture += runoff ;
            }
            else if ( elevationDelta == 0 ) {
                cellClimate.moisture -= seepage ;
                neighborClimate.moisture += seepage ;
            }
            nextClimateList[ neighbor.Index ] = neighborClimate ;
        }

        ClimateData nextCellClimate = nextClimateList[ cellIndex ] ;
        nextCellClimate.moisture += cellClimate.moisture ;
        nextClimateList[ cellIndex ] = nextCellClimate ;
        if ( nextCellClimate.moisture > 1f ) nextCellClimate.moisture = 1f ;
        climateList[ cellIndex ] = new ClimateData();
    }

    #endregion

    #region 温度

    public enum HemisphereMode {
        Both,North,South,
    }

    private int temperatureJitterChannel ;

    private float DetermineTemperature( HexCell cell ) {
        float latitude = (float) cell.coordinates.Z / hexGrid.cellCountZ ;
        if ( hemisphere == HemisphereMode.Both ) {
            latitude *= 2f ;
            if ( latitude > 1f ) latitude = 2 - latitude ;
        }
        else if ( hemisphere == HemisphereMode.North ) {
            latitude = 1f - latitude ;
        }
        float temoerature = Mathf.LerpUnclamped( lowTemperature , highTemperature , latitude ) ;
        temoerature *= 1f - (cell.ViewElevation - waterLevel) / (elevationMaxmum - waterLevel + 1f) ;
        float jitter = HexMetrics.SampleNoise( cell.Position * 0.1f )[ temperatureJitterChannel ] ;
        temoerature += (jitter * 2f - 1f) * temperatureJitter ;
        return temoerature;
    }

    #endregion

    #region 湖和河流

    private void CreateRivers() {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get() ;
        for ( int i = 0 ; i < cellCount ; i++ ) {
            HexCell cell = hexGrid.GetCell( i ) ;
            if ( cell.IsUnderWater ) continue ;

            ClimateData data = climateList[ i ] ;
            float weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMaxmum - waterLevel) ;
            if ( weight > 0.75f ) {
                riverOrigins.Add( cell );
                riverOrigins.Add( cell );
            }
            else if ( weight > 0.5f ) {
                riverOrigins.Add( cell );
            }
            else if ( weight > 0.25f ) {
                riverOrigins.Add( cell );
            }
        }

        int riverBudget = Mathf.RoundToInt( landCells * riverPercentage * 0.01f ) ;

        while ( riverBudget >0 && riverOrigins.Count>0 ) {
            int index = Random.Range( 0 , riverOrigins.Count ) ;
            int lastIndex = riverOrigins.Count - 1 ;
            HexCell origin = riverOrigins[ index ] ;
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if ( !origin.HasRiver ) {
                bool isValidOrigin = true ;
                for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                    HexCell neighbor = origin.GetNeighbor( d ) ;
                    if ( neighbor && (neighbor.HasRiver || neighbor.IsUnderWater) ) {
                        isValidOrigin = false ;
                        break ;
                    }
                }
                if(isValidOrigin) riverBudget -= CreateRiver( origin ) ;
            }
        }

        if ( riverBudget > 0 ) {
            Debug.LogWarning( "Failed to use up river budget" );
        }

        ListPool<HexCell>.Add( riverOrigins );
    }

    private List<HexDirectionEnum> flowDirections = new List<HexDirectionEnum>() ;

    private int CreateRiver( HexCell origin ) {
        int length = 1 ;
        HexCell cell = origin ;
        HexDirectionEnum direction = HexDirectionEnum.BottomLeft;
        while ( !cell.IsUnderWater ) {
            int minNeighbirElevation = int.MaxValue ;
            flowDirections.Clear();
            for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                HexCell neighbor = cell.GetNeighbor( d ) ;

                if ( !neighbor ) continue ;
                if ( neighbor.Elevation < minNeighbirElevation ) minNeighbirElevation = neighbor.Elevation ;
                if ( neighbor == origin || neighbor.HasInComingRiver ) continue ;

                int delta = neighbor.Elevation - cell.Elevation ;
                if ( delta > 0 ) continue ;

                if ( neighbor.HasOutGoingRive ) {
                    cell.SetOutGoingRiver( d );
                    return length ;
                }
                if (neighbor.IsUnderWater) {
                    cell.SetOutGoingRiver(d);
                    return length;
                }

                if ( delta <= 0 ) {
                    flowDirections.Add( d );
                    flowDirections.Add( d );
                    flowDirections.Add( d );
                }
                if ( length == 1 || (d != direction.Next( 2 ) && d != direction.Previous( 2 )) ) {
                    flowDirections.Add( d );
                }

                flowDirections.Add( d );
            }

            if ( flowDirections.Count <= 0 ) {
                if ( length == 1 ) return 0 ;
                if ( minNeighbirElevation >= cell.Elevation ) {
                    cell.WaterLevel = minNeighbirElevation ;
                    if ( minNeighbirElevation == cell.Elevation ) {
                        cell.Elevation = minNeighbirElevation - 1 ;
                    }
                }
                break ;
            }

            direction = flowDirections[ Random.Range( 0 , flowDirections.Count ) ] ;
            cell.SetOutGoingRiver( direction );
            length += 1 ;

            if ( minNeighbirElevation >= cell.Elevation && Random.value < extraLakeProbability) {
                cell.WaterLevel = cell.Elevation ;
                cell.Elevation -= 1 ;
            }
            cell = cell.GetNeighbor( direction ) ;
            
        }

        return length ;
    }

    #endregion

    #region 生物

    private float[] temperatureBands = {0.1f , 0.3f , 0.6f} ;
    private float[] moistureBands = {0.12f , 0.28f , 0.85f} ;

    struct Biome {
        public int terrain ;
        public int plant ;

        public Biome( int _terrain ,int _plant) {
            terrain = _terrain;
            plant = _plant ;
        }
    }

    /*
    *  水分为x轴，温度为Y轴
    *    _____________________
    *    | 12 | 13  | 14  | 15 |       //0,4,8,12 沙漠
    * 0.6|————|—————|—————|————|       //1，2，3， 雪
    *    |  8 |  9  | 10  | 11 |       //5，6，7， 泥
    * 0.3|————|—————|—————|————|       //9，10，11，13，14，15 草地
    *    |  4 |  5  |  6  |  7 |
    * 0.1|————|—————|—————|————|
    *    |  0 |  1  |  2  |  3 |
    *    |____|_____|_____|____|
    *        0.12  0.28  0.85
    */
    private Biome[] biomes = {   new Biome( 0 , 0 ) , new Biome( 4 , 0 ) , new Biome( 4 , 0 ) , new Biome( 4 , 0 ) ,
                                 new Biome( 0 , 0 ) , new Biome( 2 , 0 ) , new Biome( 2 , 1 ) , new Biome( 2 , 2 ) ,
                                 new Biome( 0 , 0 ) , new Biome( 1 , 0 ) , new Biome( 1 , 1 ) , new Biome( 1 , 2 ) ,
                                 new Biome( 0 , 0 ) , new Biome( 1 , 1 ) , new Biome( 1 , 2 ) , new Biome( 1 , 3 )
                             } ;

    #endregion

    private void SetTerrainType() {
        temperatureJitterChannel = Random.Range( 0 , 4 ) ;
        int rockDesertElevation = elevationMaxmum - (elevationMaxmum - waterLevel) / 2 ;
        for ( int i = 0 ; i < cellCount ; i++ ) {
            HexCell cell = hexGrid.GetCell( i ) ;
            float temperature = DetermineTemperature( cell ) ;
            //cell.SetMapData( temperature );

            float moisture = climateList[ i ].moisture ;
            if ( !cell.IsUnderWater ) {
                //cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
                /*if ( moisture < 0.05f ) cell.TerrainTypeIndex = 4 ;
                else if ( moisture < 0.12f ) cell.TerrainTypeIndex = 0 ;
                else if ( moisture < 0.28f ) cell.TerrainTypeIndex = 3 ;
                else if ( moisture < 0.85f ) cell.TerrainTypeIndex = 1 ;*/


                int t = 0 ;
                for ( ; t < temperatureBands.Length ; t++ ) {
                    if ( temperature < temperatureBands[ t ] ) break ;
                }
                int m = 0 ;
                for ( ; m < moistureBands.Length ; m++ ) {
                    if ( moisture < moistureBands[ m ] ) break ;
                }
                Biome cellBiome = biomes[ t * 4 + m ] ;
                if ( cellBiome.terrain == 0 ) {
                    if ( cell.Elevation >= rockDesertElevation ) {
                        cellBiome.terrain = 3 ;
                    }
                    if ( cell.Elevation == elevationMaxmum ) cellBiome.terrain = 4 ;
                }
                if ( cellBiome.terrain == 4 ) cellBiome.plant = 0 ;
                else if ( cellBiome.plant < 3 && cell.HasRiver ) cell.PlantLevel += 1 ;
                cell.TerrainTypeIndex = cellBiome.terrain ;
                cell.PlantLevel = cellBiome.plant ;
            }
            else {
                int terrain ;
                if ( cell.Elevation == waterLevel - 1 ) {
                    int cliffs = 0 , slopes = 0 ;
                    for ( HexDirectionEnum d = 0 ; d < HexDirectionEnum.Length ; d++ ) {
                        HexCell neighbor = cell.GetNeighbor( d ) ;
                        if ( !neighbor ) continue ;
                        int delta = neighbor.Elevation - cell.WaterLevel ;
                        if ( delta == 0 ) slopes += 1 ;
                        else if ( delta > 0 ) cliffs += 1 ;
                    }
                    if ( cliffs + slopes > 3 ) terrain = 1 ;
                    else if ( cliffs > 0 ) terrain = 3 ;
                    else if ( slopes > 0 ) terrain = 0 ;
                    else terrain = 1 ;
                }
                else if ( cell.Elevation >= waterLevel ) terrain = 1 ;
                else if ( cell.Elevation < 0 ) terrain = 3 ;
                else terrain = 2 ;
                if ( terrain == 1 && temperature < temperatureBands[ 0 ] ) terrain = 2 ;
                cell.TerrainTypeIndex = terrain;
            }

            //cell.SetMapData( (cell.Elevation - elevationMaxmum / (float) (elevationMinimum - elevationMinimum)) ) ;
            //cell.SetMapData(climateList[i].clouds);
            //cell.SetMapData(climateList[i].moisture);
            /*float data = moisture * (cell.Elevation - waterLevel) / (elevationMaxmum - waterLevel);
            if(data >0.75f) cell.SetMapData( 1f );
            else if(data > 0.5f) cell.SetMapData( 0.5f );
            else if(data >0.25f)cell.SetMapData( 0.25f );*/
            //cell.SetMapData(data);
        }
    }

    
}
