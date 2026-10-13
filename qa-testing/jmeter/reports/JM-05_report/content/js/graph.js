/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
$(document).ready(function() {

    $(".click-title").mouseenter( function(    e){
        e.preventDefault();
        this.style.cursor="pointer";
    });
    $(".click-title").mousedown( function(event){
        event.preventDefault();
    });

    // Ugly code while this script is shared among several pages
    try{
        refreshHitsPerSecond(true);
    } catch(e){}
    try{
        refreshResponseTimeOverTime(true);
    } catch(e){}
    try{
        refreshResponseTimePercentiles();
    } catch(e){}
});


var responseTimePercentilesInfos = {
        data: {"result": {"minY": 308.0, "minX": 0.0, "maxY": 715.0, "series": [{"data": [[0.0, 308.0], [0.1, 308.0], [0.2, 308.0], [0.3, 308.0], [0.4, 308.0], [0.5, 308.0], [0.6, 308.0], [0.7, 308.0], [0.8, 308.0], [0.9, 308.0], [1.0, 308.0], [1.1, 308.0], [1.2, 308.0], [1.3, 308.0], [1.4, 308.0], [1.5, 308.0], [1.6, 308.0], [1.7, 308.0], [1.8, 308.0], [1.9, 308.0], [2.0, 308.0], [2.1, 308.0], [2.2, 308.0], [2.3, 308.0], [2.4, 308.0], [2.5, 308.0], [2.6, 308.0], [2.7, 308.0], [2.8, 308.0], [2.9, 308.0], [3.0, 308.0], [3.1, 308.0], [3.2, 308.0], [3.3, 308.0], [3.4, 308.0], [3.5, 308.0], [3.6, 308.0], [3.7, 308.0], [3.8, 308.0], [3.9, 308.0], [4.0, 308.0], [4.1, 308.0], [4.2, 308.0], [4.3, 308.0], [4.4, 308.0], [4.5, 308.0], [4.6, 308.0], [4.7, 308.0], [4.8, 308.0], [4.9, 308.0], [5.0, 312.0], [5.1, 312.0], [5.2, 312.0], [5.3, 312.0], [5.4, 312.0], [5.5, 312.0], [5.6, 312.0], [5.7, 312.0], [5.8, 312.0], [5.9, 312.0], [6.0, 312.0], [6.1, 312.0], [6.2, 312.0], [6.3, 312.0], [6.4, 312.0], [6.5, 312.0], [6.6, 312.0], [6.7, 312.0], [6.8, 312.0], [6.9, 312.0], [7.0, 312.0], [7.1, 312.0], [7.2, 312.0], [7.3, 312.0], [7.4, 312.0], [7.5, 312.0], [7.6, 312.0], [7.7, 312.0], [7.8, 312.0], [7.9, 312.0], [8.0, 312.0], [8.1, 312.0], [8.2, 312.0], [8.3, 312.0], [8.4, 312.0], [8.5, 312.0], [8.6, 312.0], [8.7, 312.0], [8.8, 312.0], [8.9, 312.0], [9.0, 312.0], [9.1, 312.0], [9.2, 312.0], [9.3, 312.0], [9.4, 312.0], [9.5, 312.0], [9.6, 312.0], [9.7, 312.0], [9.8, 312.0], [9.9, 312.0], [10.0, 316.0], [10.1, 316.0], [10.2, 316.0], [10.3, 316.0], [10.4, 316.0], [10.5, 316.0], [10.6, 316.0], [10.7, 316.0], [10.8, 316.0], [10.9, 316.0], [11.0, 316.0], [11.1, 316.0], [11.2, 316.0], [11.3, 316.0], [11.4, 316.0], [11.5, 316.0], [11.6, 316.0], [11.7, 316.0], [11.8, 316.0], [11.9, 316.0], [12.0, 316.0], [12.1, 316.0], [12.2, 316.0], [12.3, 316.0], [12.4, 316.0], [12.5, 316.0], [12.6, 316.0], [12.7, 316.0], [12.8, 316.0], [12.9, 316.0], [13.0, 316.0], [13.1, 316.0], [13.2, 316.0], [13.3, 316.0], [13.4, 316.0], [13.5, 316.0], [13.6, 316.0], [13.7, 316.0], [13.8, 316.0], [13.9, 316.0], [14.0, 316.0], [14.1, 316.0], [14.2, 316.0], [14.3, 316.0], [14.4, 316.0], [14.5, 316.0], [14.6, 316.0], [14.7, 316.0], [14.8, 316.0], [14.9, 316.0], [15.0, 326.0], [15.1, 326.0], [15.2, 326.0], [15.3, 326.0], [15.4, 326.0], [15.5, 326.0], [15.6, 326.0], [15.7, 326.0], [15.8, 326.0], [15.9, 326.0], [16.0, 326.0], [16.1, 326.0], [16.2, 326.0], [16.3, 326.0], [16.4, 326.0], [16.5, 326.0], [16.6, 326.0], [16.7, 326.0], [16.8, 326.0], [16.9, 326.0], [17.0, 326.0], [17.1, 326.0], [17.2, 326.0], [17.3, 326.0], [17.4, 326.0], [17.5, 326.0], [17.6, 326.0], [17.7, 326.0], [17.8, 326.0], [17.9, 326.0], [18.0, 326.0], [18.1, 326.0], [18.2, 326.0], [18.3, 326.0], [18.4, 326.0], [18.5, 326.0], [18.6, 326.0], [18.7, 326.0], [18.8, 326.0], [18.9, 326.0], [19.0, 326.0], [19.1, 326.0], [19.2, 326.0], [19.3, 326.0], [19.4, 326.0], [19.5, 326.0], [19.6, 326.0], [19.7, 326.0], [19.8, 326.0], [19.9, 326.0], [20.0, 327.0], [20.1, 327.0], [20.2, 327.0], [20.3, 327.0], [20.4, 327.0], [20.5, 327.0], [20.6, 327.0], [20.7, 327.0], [20.8, 327.0], [20.9, 327.0], [21.0, 327.0], [21.1, 327.0], [21.2, 327.0], [21.3, 327.0], [21.4, 327.0], [21.5, 327.0], [21.6, 327.0], [21.7, 327.0], [21.8, 327.0], [21.9, 327.0], [22.0, 327.0], [22.1, 327.0], [22.2, 327.0], [22.3, 327.0], [22.4, 327.0], [22.5, 327.0], [22.6, 327.0], [22.7, 327.0], [22.8, 327.0], [22.9, 327.0], [23.0, 327.0], [23.1, 327.0], [23.2, 327.0], [23.3, 327.0], [23.4, 327.0], [23.5, 327.0], [23.6, 327.0], [23.7, 327.0], [23.8, 327.0], [23.9, 327.0], [24.0, 327.0], [24.1, 327.0], [24.2, 327.0], [24.3, 327.0], [24.4, 327.0], [24.5, 327.0], [24.6, 327.0], [24.7, 327.0], [24.8, 327.0], [24.9, 327.0], [25.0, 331.0], [25.1, 331.0], [25.2, 331.0], [25.3, 331.0], [25.4, 331.0], [25.5, 331.0], [25.6, 331.0], [25.7, 331.0], [25.8, 331.0], [25.9, 331.0], [26.0, 331.0], [26.1, 331.0], [26.2, 331.0], [26.3, 331.0], [26.4, 331.0], [26.5, 331.0], [26.6, 331.0], [26.7, 331.0], [26.8, 331.0], [26.9, 331.0], [27.0, 331.0], [27.1, 331.0], [27.2, 331.0], [27.3, 331.0], [27.4, 331.0], [27.5, 331.0], [27.6, 331.0], [27.7, 331.0], [27.8, 331.0], [27.9, 331.0], [28.0, 331.0], [28.1, 331.0], [28.2, 331.0], [28.3, 331.0], [28.4, 331.0], [28.5, 331.0], [28.6, 331.0], [28.7, 331.0], [28.8, 331.0], [28.9, 331.0], [29.0, 331.0], [29.1, 331.0], [29.2, 331.0], [29.3, 331.0], [29.4, 331.0], [29.5, 331.0], [29.6, 331.0], [29.7, 331.0], [29.8, 331.0], [29.9, 331.0], [30.0, 331.0], [30.1, 331.0], [30.2, 331.0], [30.3, 331.0], [30.4, 331.0], [30.5, 331.0], [30.6, 331.0], [30.7, 331.0], [30.8, 331.0], [30.9, 331.0], [31.0, 331.0], [31.1, 331.0], [31.2, 331.0], [31.3, 331.0], [31.4, 331.0], [31.5, 331.0], [31.6, 331.0], [31.7, 331.0], [31.8, 331.0], [31.9, 331.0], [32.0, 331.0], [32.1, 331.0], [32.2, 331.0], [32.3, 331.0], [32.4, 331.0], [32.5, 331.0], [32.6, 331.0], [32.7, 331.0], [32.8, 331.0], [32.9, 331.0], [33.0, 331.0], [33.1, 331.0], [33.2, 331.0], [33.3, 331.0], [33.4, 331.0], [33.5, 331.0], [33.6, 331.0], [33.7, 331.0], [33.8, 331.0], [33.9, 331.0], [34.0, 331.0], [34.1, 331.0], [34.2, 331.0], [34.3, 331.0], [34.4, 331.0], [34.5, 331.0], [34.6, 331.0], [34.7, 331.0], [34.8, 331.0], [34.9, 331.0], [35.0, 335.0], [35.1, 335.0], [35.2, 335.0], [35.3, 335.0], [35.4, 335.0], [35.5, 335.0], [35.6, 335.0], [35.7, 335.0], [35.8, 335.0], [35.9, 335.0], [36.0, 335.0], [36.1, 335.0], [36.2, 335.0], [36.3, 335.0], [36.4, 335.0], [36.5, 335.0], [36.6, 335.0], [36.7, 335.0], [36.8, 335.0], [36.9, 335.0], [37.0, 335.0], [37.1, 335.0], [37.2, 335.0], [37.3, 335.0], [37.4, 335.0], [37.5, 335.0], [37.6, 335.0], [37.7, 335.0], [37.8, 335.0], [37.9, 335.0], [38.0, 335.0], [38.1, 335.0], [38.2, 335.0], [38.3, 335.0], [38.4, 335.0], [38.5, 335.0], [38.6, 335.0], [38.7, 335.0], [38.8, 335.0], [38.9, 335.0], [39.0, 335.0], [39.1, 335.0], [39.2, 335.0], [39.3, 335.0], [39.4, 335.0], [39.5, 335.0], [39.6, 335.0], [39.7, 335.0], [39.8, 335.0], [39.9, 335.0], [40.0, 335.0], [40.1, 335.0], [40.2, 335.0], [40.3, 335.0], [40.4, 335.0], [40.5, 335.0], [40.6, 335.0], [40.7, 335.0], [40.8, 335.0], [40.9, 335.0], [41.0, 335.0], [41.1, 335.0], [41.2, 335.0], [41.3, 335.0], [41.4, 335.0], [41.5, 335.0], [41.6, 335.0], [41.7, 335.0], [41.8, 335.0], [41.9, 335.0], [42.0, 335.0], [42.1, 335.0], [42.2, 335.0], [42.3, 335.0], [42.4, 335.0], [42.5, 335.0], [42.6, 335.0], [42.7, 335.0], [42.8, 335.0], [42.9, 335.0], [43.0, 335.0], [43.1, 335.0], [43.2, 335.0], [43.3, 335.0], [43.4, 335.0], [43.5, 335.0], [43.6, 335.0], [43.7, 335.0], [43.8, 335.0], [43.9, 335.0], [44.0, 335.0], [44.1, 335.0], [44.2, 335.0], [44.3, 335.0], [44.4, 335.0], [44.5, 335.0], [44.6, 335.0], [44.7, 335.0], [44.8, 335.0], [44.9, 335.0], [45.0, 342.0], [45.1, 342.0], [45.2, 342.0], [45.3, 342.0], [45.4, 342.0], [45.5, 342.0], [45.6, 342.0], [45.7, 342.0], [45.8, 342.0], [45.9, 342.0], [46.0, 342.0], [46.1, 342.0], [46.2, 342.0], [46.3, 342.0], [46.4, 342.0], [46.5, 342.0], [46.6, 342.0], [46.7, 342.0], [46.8, 342.0], [46.9, 342.0], [47.0, 342.0], [47.1, 342.0], [47.2, 342.0], [47.3, 342.0], [47.4, 342.0], [47.5, 342.0], [47.6, 342.0], [47.7, 342.0], [47.8, 342.0], [47.9, 342.0], [48.0, 342.0], [48.1, 342.0], [48.2, 342.0], [48.3, 342.0], [48.4, 342.0], [48.5, 342.0], [48.6, 342.0], [48.7, 342.0], [48.8, 342.0], [48.9, 342.0], [49.0, 342.0], [49.1, 342.0], [49.2, 342.0], [49.3, 342.0], [49.4, 342.0], [49.5, 342.0], [49.6, 342.0], [49.7, 342.0], [49.8, 342.0], [49.9, 342.0], [50.0, 345.0], [50.1, 345.0], [50.2, 345.0], [50.3, 345.0], [50.4, 345.0], [50.5, 345.0], [50.6, 345.0], [50.7, 345.0], [50.8, 345.0], [50.9, 345.0], [51.0, 345.0], [51.1, 345.0], [51.2, 345.0], [51.3, 345.0], [51.4, 345.0], [51.5, 345.0], [51.6, 345.0], [51.7, 345.0], [51.8, 345.0], [51.9, 345.0], [52.0, 345.0], [52.1, 345.0], [52.2, 345.0], [52.3, 345.0], [52.4, 345.0], [52.5, 345.0], [52.6, 345.0], [52.7, 345.0], [52.8, 345.0], [52.9, 345.0], [53.0, 345.0], [53.1, 345.0], [53.2, 345.0], [53.3, 345.0], [53.4, 345.0], [53.5, 345.0], [53.6, 345.0], [53.7, 345.0], [53.8, 345.0], [53.9, 345.0], [54.0, 345.0], [54.1, 345.0], [54.2, 345.0], [54.3, 345.0], [54.4, 345.0], [54.5, 345.0], [54.6, 345.0], [54.7, 345.0], [54.8, 345.0], [54.9, 345.0], [55.0, 347.0], [55.1, 347.0], [55.2, 347.0], [55.3, 347.0], [55.4, 347.0], [55.5, 347.0], [55.6, 347.0], [55.7, 347.0], [55.8, 347.0], [55.9, 347.0], [56.0, 347.0], [56.1, 347.0], [56.2, 347.0], [56.3, 347.0], [56.4, 347.0], [56.5, 347.0], [56.6, 347.0], [56.7, 347.0], [56.8, 347.0], [56.9, 347.0], [57.0, 347.0], [57.1, 347.0], [57.2, 347.0], [57.3, 347.0], [57.4, 347.0], [57.5, 347.0], [57.6, 347.0], [57.7, 347.0], [57.8, 347.0], [57.9, 347.0], [58.0, 347.0], [58.1, 347.0], [58.2, 347.0], [58.3, 347.0], [58.4, 347.0], [58.5, 347.0], [58.6, 347.0], [58.7, 347.0], [58.8, 347.0], [58.9, 347.0], [59.0, 347.0], [59.1, 347.0], [59.2, 347.0], [59.3, 347.0], [59.4, 347.0], [59.5, 347.0], [59.6, 347.0], [59.7, 347.0], [59.8, 347.0], [59.9, 347.0], [60.0, 350.0], [60.1, 350.0], [60.2, 350.0], [60.3, 350.0], [60.4, 350.0], [60.5, 350.0], [60.6, 350.0], [60.7, 350.0], [60.8, 350.0], [60.9, 350.0], [61.0, 350.0], [61.1, 350.0], [61.2, 350.0], [61.3, 350.0], [61.4, 350.0], [61.5, 350.0], [61.6, 350.0], [61.7, 350.0], [61.8, 350.0], [61.9, 350.0], [62.0, 350.0], [62.1, 350.0], [62.2, 350.0], [62.3, 350.0], [62.4, 350.0], [62.5, 350.0], [62.6, 350.0], [62.7, 350.0], [62.8, 350.0], [62.9, 350.0], [63.0, 350.0], [63.1, 350.0], [63.2, 350.0], [63.3, 350.0], [63.4, 350.0], [63.5, 350.0], [63.6, 350.0], [63.7, 350.0], [63.8, 350.0], [63.9, 350.0], [64.0, 350.0], [64.1, 350.0], [64.2, 350.0], [64.3, 350.0], [64.4, 350.0], [64.5, 350.0], [64.6, 350.0], [64.7, 350.0], [64.8, 350.0], [64.9, 350.0], [65.0, 350.0], [65.1, 350.0], [65.2, 350.0], [65.3, 350.0], [65.4, 350.0], [65.5, 350.0], [65.6, 350.0], [65.7, 350.0], [65.8, 350.0], [65.9, 350.0], [66.0, 350.0], [66.1, 350.0], [66.2, 350.0], [66.3, 350.0], [66.4, 350.0], [66.5, 350.0], [66.6, 350.0], [66.7, 350.0], [66.8, 350.0], [66.9, 350.0], [67.0, 350.0], [67.1, 350.0], [67.2, 350.0], [67.3, 350.0], [67.4, 350.0], [67.5, 350.0], [67.6, 350.0], [67.7, 350.0], [67.8, 350.0], [67.9, 350.0], [68.0, 350.0], [68.1, 350.0], [68.2, 350.0], [68.3, 350.0], [68.4, 350.0], [68.5, 350.0], [68.6, 350.0], [68.7, 350.0], [68.8, 350.0], [68.9, 350.0], [69.0, 350.0], [69.1, 350.0], [69.2, 350.0], [69.3, 350.0], [69.4, 350.0], [69.5, 350.0], [69.6, 350.0], [69.7, 350.0], [69.8, 350.0], [69.9, 350.0], [70.0, 351.0], [70.1, 351.0], [70.2, 351.0], [70.3, 351.0], [70.4, 351.0], [70.5, 351.0], [70.6, 351.0], [70.7, 351.0], [70.8, 351.0], [70.9, 351.0], [71.0, 351.0], [71.1, 351.0], [71.2, 351.0], [71.3, 351.0], [71.4, 351.0], [71.5, 351.0], [71.6, 351.0], [71.7, 351.0], [71.8, 351.0], [71.9, 351.0], [72.0, 351.0], [72.1, 351.0], [72.2, 351.0], [72.3, 351.0], [72.4, 351.0], [72.5, 351.0], [72.6, 351.0], [72.7, 351.0], [72.8, 351.0], [72.9, 351.0], [73.0, 351.0], [73.1, 351.0], [73.2, 351.0], [73.3, 351.0], [73.4, 351.0], [73.5, 351.0], [73.6, 351.0], [73.7, 351.0], [73.8, 351.0], [73.9, 351.0], [74.0, 351.0], [74.1, 351.0], [74.2, 351.0], [74.3, 351.0], [74.4, 351.0], [74.5, 351.0], [74.6, 351.0], [74.7, 351.0], [74.8, 351.0], [74.9, 351.0], [75.0, 382.0], [75.1, 382.0], [75.2, 382.0], [75.3, 382.0], [75.4, 382.0], [75.5, 382.0], [75.6, 382.0], [75.7, 382.0], [75.8, 382.0], [75.9, 382.0], [76.0, 382.0], [76.1, 382.0], [76.2, 382.0], [76.3, 382.0], [76.4, 382.0], [76.5, 382.0], [76.6, 382.0], [76.7, 382.0], [76.8, 382.0], [76.9, 382.0], [77.0, 382.0], [77.1, 382.0], [77.2, 382.0], [77.3, 382.0], [77.4, 382.0], [77.5, 382.0], [77.6, 382.0], [77.7, 382.0], [77.8, 382.0], [77.9, 382.0], [78.0, 382.0], [78.1, 382.0], [78.2, 382.0], [78.3, 382.0], [78.4, 382.0], [78.5, 382.0], [78.6, 382.0], [78.7, 382.0], [78.8, 382.0], [78.9, 382.0], [79.0, 382.0], [79.1, 382.0], [79.2, 382.0], [79.3, 382.0], [79.4, 382.0], [79.5, 382.0], [79.6, 382.0], [79.7, 382.0], [79.8, 382.0], [79.9, 382.0], [80.0, 440.0], [80.1, 440.0], [80.2, 440.0], [80.3, 440.0], [80.4, 440.0], [80.5, 440.0], [80.6, 440.0], [80.7, 440.0], [80.8, 440.0], [80.9, 440.0], [81.0, 440.0], [81.1, 440.0], [81.2, 440.0], [81.3, 440.0], [81.4, 440.0], [81.5, 440.0], [81.6, 440.0], [81.7, 440.0], [81.8, 440.0], [81.9, 440.0], [82.0, 440.0], [82.1, 440.0], [82.2, 440.0], [82.3, 440.0], [82.4, 440.0], [82.5, 440.0], [82.6, 440.0], [82.7, 440.0], [82.8, 440.0], [82.9, 440.0], [83.0, 440.0], [83.1, 440.0], [83.2, 440.0], [83.3, 440.0], [83.4, 440.0], [83.5, 440.0], [83.6, 440.0], [83.7, 440.0], [83.8, 440.0], [83.9, 440.0], [84.0, 440.0], [84.1, 440.0], [84.2, 440.0], [84.3, 440.0], [84.4, 440.0], [84.5, 440.0], [84.6, 440.0], [84.7, 440.0], [84.8, 440.0], [84.9, 440.0], [85.0, 584.0], [85.1, 584.0], [85.2, 584.0], [85.3, 584.0], [85.4, 584.0], [85.5, 584.0], [85.6, 584.0], [85.7, 584.0], [85.8, 584.0], [85.9, 584.0], [86.0, 584.0], [86.1, 584.0], [86.2, 584.0], [86.3, 584.0], [86.4, 584.0], [86.5, 584.0], [86.6, 584.0], [86.7, 584.0], [86.8, 584.0], [86.9, 584.0], [87.0, 584.0], [87.1, 584.0], [87.2, 584.0], [87.3, 584.0], [87.4, 584.0], [87.5, 584.0], [87.6, 584.0], [87.7, 584.0], [87.8, 584.0], [87.9, 584.0], [88.0, 584.0], [88.1, 584.0], [88.2, 584.0], [88.3, 584.0], [88.4, 584.0], [88.5, 584.0], [88.6, 584.0], [88.7, 584.0], [88.8, 584.0], [88.9, 584.0], [89.0, 584.0], [89.1, 584.0], [89.2, 584.0], [89.3, 584.0], [89.4, 584.0], [89.5, 584.0], [89.6, 584.0], [89.7, 584.0], [89.8, 584.0], [89.9, 584.0], [90.0, 584.0], [90.1, 584.0], [90.2, 584.0], [90.3, 584.0], [90.4, 584.0], [90.5, 584.0], [90.6, 584.0], [90.7, 584.0], [90.8, 584.0], [90.9, 584.0], [91.0, 584.0], [91.1, 584.0], [91.2, 584.0], [91.3, 584.0], [91.4, 584.0], [91.5, 584.0], [91.6, 584.0], [91.7, 584.0], [91.8, 584.0], [91.9, 584.0], [92.0, 584.0], [92.1, 584.0], [92.2, 584.0], [92.3, 584.0], [92.4, 584.0], [92.5, 584.0], [92.6, 584.0], [92.7, 584.0], [92.8, 584.0], [92.9, 584.0], [93.0, 584.0], [93.1, 584.0], [93.2, 584.0], [93.3, 584.0], [93.4, 584.0], [93.5, 584.0], [93.6, 584.0], [93.7, 584.0], [93.8, 584.0], [93.9, 584.0], [94.0, 584.0], [94.1, 584.0], [94.2, 584.0], [94.3, 584.0], [94.4, 584.0], [94.5, 584.0], [94.6, 584.0], [94.7, 584.0], [94.8, 584.0], [94.9, 584.0], [95.0, 715.0], [95.1, 715.0], [95.2, 715.0], [95.3, 715.0], [95.4, 715.0], [95.5, 715.0], [95.6, 715.0], [95.7, 715.0], [95.8, 715.0], [95.9, 715.0], [96.0, 715.0], [96.1, 715.0], [96.2, 715.0], [96.3, 715.0], [96.4, 715.0], [96.5, 715.0], [96.6, 715.0], [96.7, 715.0], [96.8, 715.0], [96.9, 715.0], [97.0, 715.0], [97.1, 715.0], [97.2, 715.0], [97.3, 715.0], [97.4, 715.0], [97.5, 715.0], [97.6, 715.0], [97.7, 715.0], [97.8, 715.0], [97.9, 715.0], [98.0, 715.0], [98.1, 715.0], [98.2, 715.0], [98.3, 715.0], [98.4, 715.0], [98.5, 715.0], [98.6, 715.0], [98.7, 715.0], [98.8, 715.0], [98.9, 715.0], [99.0, 715.0], [99.1, 715.0], [99.2, 715.0], [99.3, 715.0], [99.4, 715.0], [99.5, 715.0], [99.6, 715.0], [99.7, 715.0], [99.8, 715.0], [99.9, 715.0]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 100.0, "title": "Response Time Percentiles"}},
        getOptions: function() {
            return {
                series: {
                    points: { show: false }
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendResponseTimePercentiles'
                },
                xaxis: {
                    tickDecimals: 1,
                    axisLabel: "Percentiles",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Percentile value in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : %x.2 percentile was %y ms"
                },
                selection: { mode: "xy" },
            };
        },
        createGraph: function() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesResponseTimePercentiles"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotResponseTimesPercentiles"), dataset, options);
            // setup overview
            $.plot($("#overviewResponseTimesPercentiles"), dataset, prepareOverviewOptions(options));
        }
};

/**
 * @param elementId Id of element where we display message
 */
function setEmptyGraph(elementId) {
    $(function() {
        $(elementId).text("No graph series with filter="+seriesFilter);
    });
}

// Response times percentiles
function refreshResponseTimePercentiles() {
    var infos = responseTimePercentilesInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyResponseTimePercentiles");
        return;
    }
    if (isGraph($("#flotResponseTimesPercentiles"))){
        infos.createGraph();
    } else {
        var choiceContainer = $("#choicesResponseTimePercentiles");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotResponseTimesPercentiles", "#overviewResponseTimesPercentiles");
        $('#bodyResponseTimePercentiles .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
}

var responseTimeDistributionInfos = {
        data: {"result": {"minY": 1.0, "minX": 300.0, "maxY": 16.0, "series": [{"data": [[300.0, 16.0], [700.0, 1.0], [400.0, 1.0], [500.0, 2.0]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 100, "maxX": 700.0, "title": "Response Time Distribution"}},
        getOptions: function() {
            var granularity = this.data.result.granularity;
            return {
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendResponseTimeDistribution'
                },
                xaxis:{
                    axisLabel: "Response times in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of responses",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                bars : {
                    show: true,
                    barWidth: this.data.result.granularity
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: function(label, xval, yval, flotItem){
                        return yval + " responses for " + label + " were between " + xval + " and " + (xval + granularity) + " ms";
                    }
                }
            };
        },
        createGraph: function() {
            var data = this.data;
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotResponseTimeDistribution"), prepareData(data.result.series, $("#choicesResponseTimeDistribution")), options);
        }

};

// Response time distribution
function refreshResponseTimeDistribution() {
    var infos = responseTimeDistributionInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyResponseTimeDistribution");
        return;
    }
    if (isGraph($("#flotResponseTimeDistribution"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesResponseTimeDistribution");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        $('#footerResponseTimeDistribution .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};


var syntheticResponseTimeDistributionInfos = {
        data: {"result": {"minY": 3.0, "minX": 0.0, "ticks": [[0, "Requests having \nresponse time <= 500ms"], [1, "Requests having \nresponse time > 500ms and <= 1,500ms"], [2, "Requests having \nresponse time > 1,500ms"], [3, "Requests in error"]], "maxY": 17.0, "series": [{"data": [[0.0, 17.0]], "color": "#9ACD32", "isOverall": false, "label": "Requests having \nresponse time <= 500ms", "isController": false}, {"data": [[1.0, 3.0]], "color": "yellow", "isOverall": false, "label": "Requests having \nresponse time > 500ms and <= 1,500ms", "isController": false}, {"data": [], "color": "orange", "isOverall": false, "label": "Requests having \nresponse time > 1,500ms", "isController": false}, {"data": [], "color": "#FF6347", "isOverall": false, "label": "Requests in error", "isController": false}], "supportsControllersDiscrimination": false, "maxX": 1.0, "title": "Synthetic Response Times Distribution"}},
        getOptions: function() {
            return {
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendSyntheticResponseTimeDistribution'
                },
                xaxis:{
                    axisLabel: "Response times ranges",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                    tickLength:0,
                    min:-0.5,
                    max:3.5
                },
                yaxis: {
                    axisLabel: "Number of responses",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                bars : {
                    show: true,
                    align: "center",
                    barWidth: 0.25,
                    fill:.75
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: function(label, xval, yval, flotItem){
                        return yval + " " + label;
                    }
                }
            };
        },
        createGraph: function() {
            var data = this.data;
            var options = this.getOptions();
            prepareOptions(options, data);
            options.xaxis.ticks = data.result.ticks;
            $.plot($("#flotSyntheticResponseTimeDistribution"), prepareData(data.result.series, $("#choicesSyntheticResponseTimeDistribution")), options);
        }

};

// Response time distribution
function refreshSyntheticResponseTimeDistribution() {
    var infos = syntheticResponseTimeDistributionInfos;
    prepareSeries(infos.data, true);
    if (isGraph($("#flotSyntheticResponseTimeDistribution"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesSyntheticResponseTimeDistribution");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        $('#footerSyntheticResponseTimeDistribution .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var activeThreadsOverTimeInfos = {
        data: {"result": {"minY": 2.0, "minX": 1.79182614E12, "maxY": 2.545454545454545, "series": [{"data": [[1.7918262E12, 2.0], [1.79182614E12, 2.545454545454545]], "isOverall": false, "label": "Thread Group", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918262E12, "title": "Active Threads Over Time"}},
        getOptions: function() {
            return {
                series: {
                    stack: true,
                    lines: {
                        show: true,
                        fill: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of active threads",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                legend: {
                    noColumns: 6,
                    show: true,
                    container: '#legendActiveThreadsOverTime'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                selection: {
                    mode: 'xy'
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : At %x there were %y active threads"
                }
            };
        },
        createGraph: function() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesActiveThreadsOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotActiveThreadsOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewActiveThreadsOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Active Threads Over Time
function refreshActiveThreadsOverTime(fixTimestamps) {
    var infos = activeThreadsOverTimeInfos;
    prepareSeries(infos.data);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotActiveThreadsOverTime"))) {
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesActiveThreadsOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotActiveThreadsOverTime", "#overviewActiveThreadsOverTime");
        $('#footerActiveThreadsOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var timeVsThreadsInfos = {
        data: {"result": {"minY": 350.0, "minX": 1.0, "maxY": 506.3333333333333, "series": [{"data": [[4.0, 506.3333333333333], [2.0, 360.1333333333334], [1.0, 350.0], [3.0, 440.0]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}, {"data": [[2.3, 385.55]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler-Aggregated", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 4.0, "title": "Time VS Threads"}},
        getOptions: function() {
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    axisLabel: "Number of active threads",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Average response times in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                legend: { noColumns: 2,show: true, container: '#legendTimeVsThreads' },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s: At %x.2 active threads, Average response time was %y.2 ms"
                }
            };
        },
        createGraph: function() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesTimeVsThreads"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotTimesVsThreads"), dataset, options);
            // setup overview
            $.plot($("#overviewTimesVsThreads"), dataset, prepareOverviewOptions(options));
        }
};

// Time vs threads
function refreshTimeVsThreads(){
    var infos = timeVsThreadsInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyTimeVsThreads");
        return;
    }
    if(isGraph($("#flotTimesVsThreads"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesTimeVsThreads");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotTimesVsThreads", "#overviewTimesVsThreads");
        $('#footerTimeVsThreads .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var bytesThroughputOverTimeInfos = {
        data : {"result": {"minY": 28.95, "minX": 1.79182614E12, "maxY": 143.36666666666667, "series": [{"data": [[1.7918262E12, 28.95], [1.79182614E12, 35.38333333333333]], "isOverall": false, "label": "Bytes received per second", "isController": false}, {"data": [[1.7918262E12, 117.3], [1.79182614E12, 143.36666666666667]], "isOverall": false, "label": "Bytes sent per second", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918262E12, "title": "Bytes Throughput Over Time"}},
        getOptions : function(){
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity) ,
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Bytes / sec",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendBytesThroughputOverTime'
                },
                selection: {
                    mode: "xy"
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s at %x was %y"
                }
            };
        },
        createGraph : function() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesBytesThroughputOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotBytesThroughputOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewBytesThroughputOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Bytes throughput Over Time
function refreshBytesThroughputOverTime(fixTimestamps) {
    var infos = bytesThroughputOverTimeInfos;
    prepareSeries(infos.data);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotBytesThroughputOverTime"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesBytesThroughputOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotBytesThroughputOverTime", "#overviewBytesThroughputOverTime");
        $('#footerBytesThroughputOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
}

var responseTimesOverTimeInfos = {
        data: {"result": {"minY": 384.45454545454544, "minX": 1.79182614E12, "maxY": 386.8888888888889, "series": [{"data": [[1.7918262E12, 386.8888888888889], [1.79182614E12, 384.45454545454544]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918262E12, "title": "Response Time Over Time"}},
        getOptions: function(){
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Average response time in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendResponseTimesOverTime'
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : at %x Average response time was %y ms"
                }
            };
        },
        createGraph: function() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesResponseTimesOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotResponseTimesOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewResponseTimesOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Response Times Over Time
function refreshResponseTimeOverTime(fixTimestamps) {
    var infos = responseTimesOverTimeInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyResponseTimeOverTime");
        return;
    }
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotResponseTimesOverTime"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesResponseTimesOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotResponseTimesOverTime", "#overviewResponseTimesOverTime");
        $('#footerResponseTimesOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var latenciesOverTimeInfos = {
        data: {"result": {"minY": 380.7272727272727, "minX": 1.79182614E12, "maxY": 386.8888888888889, "series": [{"data": [[1.7918262E12, 386.8888888888889], [1.79182614E12, 380.7272727272727]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918262E12, "title": "Latencies Over Time"}},
        getOptions: function() {
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Average response latencies in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendLatenciesOverTime'
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : at %x Average latency was %y ms"
                }
            };
        },
        createGraph: function () {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesLatenciesOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotLatenciesOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewLatenciesOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Latencies Over Time
function refreshLatenciesOverTime(fixTimestamps) {
    var infos = latenciesOverTimeInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyLatenciesOverTime");
        return;
    }
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotLatenciesOverTime"))) {
        infos.createGraph();
    }else {
        var choiceContainer = $("#choicesLatenciesOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotLatenciesOverTime", "#overviewLatenciesOverTime");
        $('#footerLatenciesOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var connectTimeOverTimeInfos = {
        data: {"result": {"minY": 2.0, "minX": 1.79182614E12, "maxY": 12.636363636363637, "series": [{"data": [[1.7918262E12, 2.0], [1.79182614E12, 12.636363636363637]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918262E12, "title": "Connect Time Over Time"}},
        getOptions: function() {
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getConnectTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Average Connect Time in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendConnectTimeOverTime'
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : at %x Average connect time was %y ms"
                }
            };
        },
        createGraph: function () {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesConnectTimeOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotConnectTimeOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewConnectTimeOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Connect Time Over Time
function refreshConnectTimeOverTime(fixTimestamps) {
    var infos = connectTimeOverTimeInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyConnectTimeOverTime");
        return;
    }
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotConnectTimeOverTime"))) {
        infos.createGraph();
    }else {
        var choiceContainer = $("#choicesConnectTimeOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotConnectTimeOverTime", "#overviewConnectTimeOverTime");
        $('#footerConnectTimeOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var responseTimePercentilesOverTimeInfos = {
        data: {"result": {"minY": 308.0, "minX": 1.79182614E12, "maxY": 715.0, "series": [{"data": [[1.7918262E12, 715.0], [1.79182614E12, 584.0]], "isOverall": false, "label": "Max", "isController": false}, {"data": [[1.7918262E12, 308.0], [1.79182614E12, 312.0]], "isOverall": false, "label": "Min", "isController": false}, {"data": [[1.7918262E12, 715.0], [1.79182614E12, 584.0]], "isOverall": false, "label": "90th percentile", "isController": false}, {"data": [[1.7918262E12, 715.0], [1.79182614E12, 584.0]], "isOverall": false, "label": "99th percentile", "isController": false}, {"data": [[1.7918262E12, 335.0], [1.79182614E12, 345.0]], "isOverall": false, "label": "Median", "isController": false}, {"data": [[1.7918262E12, 715.0], [1.79182614E12, 584.0]], "isOverall": false, "label": "95th percentile", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918262E12, "title": "Response Time Percentiles Over Time (successful requests only)"}},
        getOptions: function() {
            return {
                series: {
                    lines: {
                        show: true,
                        fill: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Response Time in ms",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: '#legendResponseTimePercentilesOverTime'
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s : at %x Response time was %y ms"
                }
            };
        },
        createGraph: function () {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesResponseTimePercentilesOverTime"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotResponseTimePercentilesOverTime"), dataset, options);
            // setup overview
            $.plot($("#overviewResponseTimePercentilesOverTime"), dataset, prepareOverviewOptions(options));
        }
};

// Response Time Percentiles Over Time
function refreshResponseTimePercentilesOverTime(fixTimestamps) {
    var infos = responseTimePercentilesOverTimeInfos;
    prepareSeries(infos.data);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotResponseTimePercentilesOverTime"))) {
        infos.createGraph();
    }else {
        var choiceContainer = $("#choicesResponseTimePercentilesOverTime");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotResponseTimePercentilesOverTime", "#overviewResponseTimePercentilesOverTime");
        $('#footerResponseTimePercentilesOverTime .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};


var responseTimeVsRequestInfos = {
    data: {"result": {"minY": 335.0, "minX": 1.0, "maxY": 584.0, "series": [{"data": [[4.0, 335.0], [1.0, 350.0], [3.0, 584.0]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 4.0, "title": "Response Time Vs Request"}},
    getOptions: function() {
        return {
            series: {
                lines: {
                    show: false
                },
                points: {
                    show: true
                }
            },
            xaxis: {
                axisLabel: "Global number of requests per second",
                axisLabelUseCanvas: true,
                axisLabelFontSizePixels: 12,
                axisLabelFontFamily: 'Verdana, Arial',
                axisLabelPadding: 20,
            },
            yaxis: {
                axisLabel: "Median Response Time in ms",
                axisLabelUseCanvas: true,
                axisLabelFontSizePixels: 12,
                axisLabelFontFamily: 'Verdana, Arial',
                axisLabelPadding: 20,
            },
            legend: {
                noColumns: 2,
                show: true,
                container: '#legendResponseTimeVsRequest'
            },
            selection: {
                mode: 'xy'
            },
            grid: {
                hoverable: true // IMPORTANT! this is needed for tooltip to work
            },
            tooltip: true,
            tooltipOpts: {
                content: "%s : Median response time at %x req/s was %y ms"
            },
            colors: ["#9ACD32", "#FF6347"]
        };
    },
    createGraph: function () {
        var data = this.data;
        var dataset = prepareData(data.result.series, $("#choicesResponseTimeVsRequest"));
        var options = this.getOptions();
        prepareOptions(options, data);
        $.plot($("#flotResponseTimeVsRequest"), dataset, options);
        // setup overview
        $.plot($("#overviewResponseTimeVsRequest"), dataset, prepareOverviewOptions(options));

    }
};

// Response Time vs Request
function refreshResponseTimeVsRequest() {
    var infos = responseTimeVsRequestInfos;
    prepareSeries(infos.data);
    if (isGraph($("#flotResponseTimeVsRequest"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesResponseTimeVsRequest");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotResponseTimeVsRequest", "#overviewResponseTimeVsRequest");
        $('#footerResponseRimeVsRequest .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};


var latenciesVsRequestInfos = {
    data: {"result": {"minY": 335.0, "minX": 1.0, "maxY": 571.0, "series": [{"data": [[4.0, 335.0], [1.0, 350.0], [3.0, 571.0]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 4.0, "title": "Latencies Vs Request"}},
    getOptions: function() {
        return{
            series: {
                lines: {
                    show: false
                },
                points: {
                    show: true
                }
            },
            xaxis: {
                axisLabel: "Global number of requests per second",
                axisLabelUseCanvas: true,
                axisLabelFontSizePixels: 12,
                axisLabelFontFamily: 'Verdana, Arial',
                axisLabelPadding: 20,
            },
            yaxis: {
                axisLabel: "Median Latency in ms",
                axisLabelUseCanvas: true,
                axisLabelFontSizePixels: 12,
                axisLabelFontFamily: 'Verdana, Arial',
                axisLabelPadding: 20,
            },
            legend: { noColumns: 2,show: true, container: '#legendLatencyVsRequest' },
            selection: {
                mode: 'xy'
            },
            grid: {
                hoverable: true // IMPORTANT! this is needed for tooltip to work
            },
            tooltip: true,
            tooltipOpts: {
                content: "%s : Median Latency time at %x req/s was %y ms"
            },
            colors: ["#9ACD32", "#FF6347"]
        };
    },
    createGraph: function () {
        var data = this.data;
        var dataset = prepareData(data.result.series, $("#choicesLatencyVsRequest"));
        var options = this.getOptions();
        prepareOptions(options, data);
        $.plot($("#flotLatenciesVsRequest"), dataset, options);
        // setup overview
        $.plot($("#overviewLatenciesVsRequest"), dataset, prepareOverviewOptions(options));
    }
};

// Latencies vs Request
function refreshLatenciesVsRequest() {
        var infos = latenciesVsRequestInfos;
        prepareSeries(infos.data);
        if(isGraph($("#flotLatenciesVsRequest"))){
            infos.createGraph();
        }else{
            var choiceContainer = $("#choicesLatencyVsRequest");
            createLegend(choiceContainer, infos);
            infos.createGraph();
            setGraphZoomable("#flotLatenciesVsRequest", "#overviewLatenciesVsRequest");
            $('#footerLatenciesVsRequest .legendColorBox > div').each(function(i){
                $(this).clone().prependTo(choiceContainer.find("li").eq(i));
            });
        }
};

var hitsPerSecondInfos = {
        data: {"result": {"minY": 0.11666666666666667, "minX": 1.79182614E12, "maxY": 0.21666666666666667, "series": [{"data": [[1.7918262E12, 0.11666666666666667], [1.79182614E12, 0.21666666666666667]], "isOverall": false, "label": "hitsPerSecond", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918262E12, "title": "Hits Per Second"}},
        getOptions: function() {
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of hits / sec",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: "#legendHitsPerSecond"
                },
                selection: {
                    mode : 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s at %x was %y.2 hits/sec"
                }
            };
        },
        createGraph: function createGraph() {
            var data = this.data;
            var dataset = prepareData(data.result.series, $("#choicesHitsPerSecond"));
            var options = this.getOptions();
            prepareOptions(options, data);
            $.plot($("#flotHitsPerSecond"), dataset, options);
            // setup overview
            $.plot($("#overviewHitsPerSecond"), dataset, prepareOverviewOptions(options));
        }
};

// Hits per second
function refreshHitsPerSecond(fixTimestamps) {
    var infos = hitsPerSecondInfos;
    prepareSeries(infos.data);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if (isGraph($("#flotHitsPerSecond"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesHitsPerSecond");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotHitsPerSecond", "#overviewHitsPerSecond");
        $('#footerHitsPerSecond .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
}

var codesPerSecondInfos = {
        data: {"result": {"minY": 0.15, "minX": 1.79182614E12, "maxY": 0.18333333333333332, "series": [{"data": [[1.7918262E12, 0.15], [1.79182614E12, 0.18333333333333332]], "isOverall": false, "label": "200", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918262E12, "title": "Codes Per Second"}},
        getOptions: function(){
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of responses / sec",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: "#legendCodesPerSecond"
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "Number of Response Codes %s at %x was %y.2 responses / sec"
                }
            };
        },
    createGraph: function() {
        var data = this.data;
        var dataset = prepareData(data.result.series, $("#choicesCodesPerSecond"));
        var options = this.getOptions();
        prepareOptions(options, data);
        $.plot($("#flotCodesPerSecond"), dataset, options);
        // setup overview
        $.plot($("#overviewCodesPerSecond"), dataset, prepareOverviewOptions(options));
    }
};

// Codes per second
function refreshCodesPerSecond(fixTimestamps) {
    var infos = codesPerSecondInfos;
    prepareSeries(infos.data);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotCodesPerSecond"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesCodesPerSecond");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotCodesPerSecond", "#overviewCodesPerSecond");
        $('#footerCodesPerSecond .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var transactionsPerSecondInfos = {
        data: {"result": {"minY": 0.15, "minX": 1.79182614E12, "maxY": 0.18333333333333332, "series": [{"data": [[1.7918262E12, 0.15], [1.79182614E12, 0.18333333333333332]], "isOverall": false, "label": "JM-05-MarkAllAsRead Sampler-success", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918262E12, "title": "Transactions Per Second"}},
        getOptions: function(){
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of transactions / sec",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: "#legendTransactionsPerSecond"
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s at %x was %y transactions / sec"
                }
            };
        },
    createGraph: function () {
        var data = this.data;
        var dataset = prepareData(data.result.series, $("#choicesTransactionsPerSecond"));
        var options = this.getOptions();
        prepareOptions(options, data);
        $.plot($("#flotTransactionsPerSecond"), dataset, options);
        // setup overview
        $.plot($("#overviewTransactionsPerSecond"), dataset, prepareOverviewOptions(options));
    }
};

// Transactions per second
function refreshTransactionsPerSecond(fixTimestamps) {
    var infos = transactionsPerSecondInfos;
    prepareSeries(infos.data);
    if(infos.data.result.series.length == 0) {
        setEmptyGraph("#bodyTransactionsPerSecond");
        return;
    }
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotTransactionsPerSecond"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesTransactionsPerSecond");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotTransactionsPerSecond", "#overviewTransactionsPerSecond");
        $('#footerTransactionsPerSecond .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

var totalTPSInfos = {
        data: {"result": {"minY": 0.15, "minX": 1.79182614E12, "maxY": 0.18333333333333332, "series": [{"data": [[1.7918262E12, 0.15], [1.79182614E12, 0.18333333333333332]], "isOverall": false, "label": "Transaction-success", "isController": false}, {"data": [], "isOverall": false, "label": "Transaction-failure", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918262E12, "title": "Total Transactions Per Second"}},
        getOptions: function(){
            return {
                series: {
                    lines: {
                        show: true
                    },
                    points: {
                        show: true
                    }
                },
                xaxis: {
                    mode: "time",
                    timeformat: getTimeFormat(this.data.result.granularity),
                    axisLabel: getElapsedTimeLabel(this.data.result.granularity),
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20,
                },
                yaxis: {
                    axisLabel: "Number of transactions / sec",
                    axisLabelUseCanvas: true,
                    axisLabelFontSizePixels: 12,
                    axisLabelFontFamily: 'Verdana, Arial',
                    axisLabelPadding: 20
                },
                legend: {
                    noColumns: 2,
                    show: true,
                    container: "#legendTotalTPS"
                },
                selection: {
                    mode: 'xy'
                },
                grid: {
                    hoverable: true // IMPORTANT! this is needed for tooltip to
                                    // work
                },
                tooltip: true,
                tooltipOpts: {
                    content: "%s at %x was %y transactions / sec"
                },
                colors: ["#9ACD32", "#FF6347"]
            };
        },
    createGraph: function () {
        var data = this.data;
        var dataset = prepareData(data.result.series, $("#choicesTotalTPS"));
        var options = this.getOptions();
        prepareOptions(options, data);
        $.plot($("#flotTotalTPS"), dataset, options);
        // setup overview
        $.plot($("#overviewTotalTPS"), dataset, prepareOverviewOptions(options));
    }
};

// Total Transactions per second
function refreshTotalTPS(fixTimestamps) {
    var infos = totalTPSInfos;
    // We want to ignore seriesFilter
    prepareSeries(infos.data, false, true);
    if(fixTimestamps) {
        fixTimeStamps(infos.data.result.series, 19800000);
    }
    if(isGraph($("#flotTotalTPS"))){
        infos.createGraph();
    }else{
        var choiceContainer = $("#choicesTotalTPS");
        createLegend(choiceContainer, infos);
        infos.createGraph();
        setGraphZoomable("#flotTotalTPS", "#overviewTotalTPS");
        $('#footerTotalTPS .legendColorBox > div').each(function(i){
            $(this).clone().prependTo(choiceContainer.find("li").eq(i));
        });
    }
};

// Collapse the graph matching the specified DOM element depending the collapsed
// status
function collapse(elem, collapsed){
    if(collapsed){
        $(elem).parent().find(".fa-chevron-up").removeClass("fa-chevron-up").addClass("fa-chevron-down");
    } else {
        $(elem).parent().find(".fa-chevron-down").removeClass("fa-chevron-down").addClass("fa-chevron-up");
        if (elem.id == "bodyBytesThroughputOverTime") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshBytesThroughputOverTime(true);
            }
            document.location.href="#bytesThroughputOverTime";
        } else if (elem.id == "bodyLatenciesOverTime") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshLatenciesOverTime(true);
            }
            document.location.href="#latenciesOverTime";
        } else if (elem.id == "bodyCustomGraph") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshCustomGraph(true);
            }
            document.location.href="#responseCustomGraph";
        } else if (elem.id == "bodyConnectTimeOverTime") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshConnectTimeOverTime(true);
            }
            document.location.href="#connectTimeOverTime";
        } else if (elem.id == "bodyResponseTimePercentilesOverTime") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshResponseTimePercentilesOverTime(true);
            }
            document.location.href="#responseTimePercentilesOverTime";
        } else if (elem.id == "bodyResponseTimeDistribution") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshResponseTimeDistribution();
            }
            document.location.href="#responseTimeDistribution" ;
        } else if (elem.id == "bodySyntheticResponseTimeDistribution") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshSyntheticResponseTimeDistribution();
            }
            document.location.href="#syntheticResponseTimeDistribution" ;
        } else if (elem.id == "bodyActiveThreadsOverTime") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshActiveThreadsOverTime(true);
            }
            document.location.href="#activeThreadsOverTime";
        } else if (elem.id == "bodyTimeVsThreads") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshTimeVsThreads();
            }
            document.location.href="#timeVsThreads" ;
        } else if (elem.id == "bodyCodesPerSecond") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshCodesPerSecond(true);
            }
            document.location.href="#codesPerSecond";
        } else if (elem.id == "bodyTransactionsPerSecond") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshTransactionsPerSecond(true);
            }
            document.location.href="#transactionsPerSecond";
        } else if (elem.id == "bodyTotalTPS") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshTotalTPS(true);
            }
            document.location.href="#totalTPS";
        } else if (elem.id == "bodyResponseTimeVsRequest") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshResponseTimeVsRequest();
            }
            document.location.href="#responseTimeVsRequest";
        } else if (elem.id == "bodyLatenciesVsRequest") {
            if (isGraph($(elem).find('.flot-chart-content')) == false) {
                refreshLatenciesVsRequest();
            }
            document.location.href="#latencyVsRequest";
        }
    }
}

/*
 * Activates or deactivates all series of the specified graph (represented by id parameter)
 * depending on checked argument.
 */
function toggleAll(id, checked){
    var placeholder = document.getElementById(id);

    var cases = $(placeholder).find(':checkbox');
    cases.prop('checked', checked);
    $(cases).parent().children().children().toggleClass("legend-disabled", !checked);

    var choiceContainer;
    if ( id == "choicesBytesThroughputOverTime"){
        choiceContainer = $("#choicesBytesThroughputOverTime");
        refreshBytesThroughputOverTime(false);
    } else if(id == "choicesResponseTimesOverTime"){
        choiceContainer = $("#choicesResponseTimesOverTime");
        refreshResponseTimeOverTime(false);
    }else if(id == "choicesResponseCustomGraph"){
        choiceContainer = $("#choicesResponseCustomGraph");
        refreshCustomGraph(false);
    } else if ( id == "choicesLatenciesOverTime"){
        choiceContainer = $("#choicesLatenciesOverTime");
        refreshLatenciesOverTime(false);
    } else if ( id == "choicesConnectTimeOverTime"){
        choiceContainer = $("#choicesConnectTimeOverTime");
        refreshConnectTimeOverTime(false);
    } else if ( id == "choicesResponseTimePercentilesOverTime"){
        choiceContainer = $("#choicesResponseTimePercentilesOverTime");
        refreshResponseTimePercentilesOverTime(false);
    } else if ( id == "choicesResponseTimePercentiles"){
        choiceContainer = $("#choicesResponseTimePercentiles");
        refreshResponseTimePercentiles();
    } else if(id == "choicesActiveThreadsOverTime"){
        choiceContainer = $("#choicesActiveThreadsOverTime");
        refreshActiveThreadsOverTime(false);
    } else if ( id == "choicesTimeVsThreads"){
        choiceContainer = $("#choicesTimeVsThreads");
        refreshTimeVsThreads();
    } else if ( id == "choicesSyntheticResponseTimeDistribution"){
        choiceContainer = $("#choicesSyntheticResponseTimeDistribution");
        refreshSyntheticResponseTimeDistribution();
    } else if ( id == "choicesResponseTimeDistribution"){
        choiceContainer = $("#choicesResponseTimeDistribution");
        refreshResponseTimeDistribution();
    } else if ( id == "choicesHitsPerSecond"){
        choiceContainer = $("#choicesHitsPerSecond");
        refreshHitsPerSecond(false);
    } else if(id == "choicesCodesPerSecond"){
        choiceContainer = $("#choicesCodesPerSecond");
        refreshCodesPerSecond(false);
    } else if ( id == "choicesTransactionsPerSecond"){
        choiceContainer = $("#choicesTransactionsPerSecond");
        refreshTransactionsPerSecond(false);
    } else if ( id == "choicesTotalTPS"){
        choiceContainer = $("#choicesTotalTPS");
        refreshTotalTPS(false);
    } else if ( id == "choicesResponseTimeVsRequest"){
        choiceContainer = $("#choicesResponseTimeVsRequest");
        refreshResponseTimeVsRequest();
    } else if ( id == "choicesLatencyVsRequest"){
        choiceContainer = $("#choicesLatencyVsRequest");
        refreshLatenciesVsRequest();
    }
    var color = checked ? "black" : "#818181";
    if(choiceContainer != null) {
        choiceContainer.find("label").each(function(){
            this.style.color = color;
        });
    }
}

