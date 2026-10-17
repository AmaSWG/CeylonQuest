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
        data: {"result": {"minY": 288.0, "minX": 0.0, "maxY": 2390.0, "series": [{"data": [[0.0, 288.0], [0.1, 288.0], [0.2, 288.0], [0.3, 288.0], [0.4, 288.0], [0.5, 288.0], [0.6, 288.0], [0.7, 288.0], [0.8, 288.0], [0.9, 288.0], [1.0, 291.0], [1.1, 291.0], [1.2, 291.0], [1.3, 291.0], [1.4, 291.0], [1.5, 291.0], [1.6, 291.0], [1.7, 291.0], [1.8, 291.0], [1.9, 291.0], [2.0, 302.0], [2.1, 302.0], [2.2, 302.0], [2.3, 302.0], [2.4, 302.0], [2.5, 302.0], [2.6, 302.0], [2.7, 302.0], [2.8, 302.0], [2.9, 302.0], [3.0, 304.0], [3.1, 304.0], [3.2, 304.0], [3.3, 304.0], [3.4, 304.0], [3.5, 304.0], [3.6, 304.0], [3.7, 304.0], [3.8, 304.0], [3.9, 304.0], [4.0, 307.0], [4.1, 307.0], [4.2, 307.0], [4.3, 307.0], [4.4, 307.0], [4.5, 307.0], [4.6, 307.0], [4.7, 307.0], [4.8, 307.0], [4.9, 307.0], [5.0, 311.0], [5.1, 311.0], [5.2, 311.0], [5.3, 311.0], [5.4, 311.0], [5.5, 311.0], [5.6, 311.0], [5.7, 311.0], [5.8, 311.0], [5.9, 311.0], [6.0, 311.0], [6.1, 311.0], [6.2, 311.0], [6.3, 311.0], [6.4, 311.0], [6.5, 311.0], [6.6, 311.0], [6.7, 311.0], [6.8, 311.0], [6.9, 311.0], [7.0, 315.0], [7.1, 315.0], [7.2, 315.0], [7.3, 315.0], [7.4, 315.0], [7.5, 315.0], [7.6, 315.0], [7.7, 315.0], [7.8, 315.0], [7.9, 315.0], [8.0, 316.0], [8.1, 316.0], [8.2, 316.0], [8.3, 316.0], [8.4, 316.0], [8.5, 316.0], [8.6, 316.0], [8.7, 316.0], [8.8, 316.0], [8.9, 316.0], [9.0, 320.0], [9.1, 320.0], [9.2, 320.0], [9.3, 320.0], [9.4, 320.0], [9.5, 320.0], [9.6, 320.0], [9.7, 320.0], [9.8, 320.0], [9.9, 320.0], [10.0, 323.0], [10.1, 323.0], [10.2, 323.0], [10.3, 323.0], [10.4, 323.0], [10.5, 323.0], [10.6, 323.0], [10.7, 323.0], [10.8, 323.0], [10.9, 323.0], [11.0, 327.0], [11.1, 327.0], [11.2, 327.0], [11.3, 327.0], [11.4, 327.0], [11.5, 327.0], [11.6, 327.0], [11.7, 327.0], [11.8, 327.0], [11.9, 327.0], [12.0, 336.0], [12.1, 336.0], [12.2, 336.0], [12.3, 336.0], [12.4, 336.0], [12.5, 336.0], [12.6, 336.0], [12.7, 336.0], [12.8, 336.0], [12.9, 336.0], [13.0, 337.0], [13.1, 337.0], [13.2, 337.0], [13.3, 337.0], [13.4, 337.0], [13.5, 337.0], [13.6, 337.0], [13.7, 337.0], [13.8, 337.0], [13.9, 337.0], [14.0, 338.0], [14.1, 338.0], [14.2, 338.0], [14.3, 338.0], [14.4, 338.0], [14.5, 338.0], [14.6, 338.0], [14.7, 338.0], [14.8, 338.0], [14.9, 338.0], [15.0, 340.0], [15.1, 340.0], [15.2, 340.0], [15.3, 340.0], [15.4, 340.0], [15.5, 340.0], [15.6, 340.0], [15.7, 340.0], [15.8, 340.0], [15.9, 340.0], [16.0, 341.0], [16.1, 341.0], [16.2, 341.0], [16.3, 341.0], [16.4, 341.0], [16.5, 341.0], [16.6, 341.0], [16.7, 341.0], [16.8, 341.0], [16.9, 341.0], [17.0, 342.0], [17.1, 342.0], [17.2, 342.0], [17.3, 342.0], [17.4, 342.0], [17.5, 342.0], [17.6, 342.0], [17.7, 342.0], [17.8, 342.0], [17.9, 342.0], [18.0, 345.0], [18.1, 345.0], [18.2, 345.0], [18.3, 345.0], [18.4, 345.0], [18.5, 345.0], [18.6, 345.0], [18.7, 345.0], [18.8, 345.0], [18.9, 345.0], [19.0, 347.0], [19.1, 347.0], [19.2, 347.0], [19.3, 347.0], [19.4, 347.0], [19.5, 347.0], [19.6, 347.0], [19.7, 347.0], [19.8, 347.0], [19.9, 347.0], [20.0, 349.0], [20.1, 349.0], [20.2, 349.0], [20.3, 349.0], [20.4, 349.0], [20.5, 349.0], [20.6, 349.0], [20.7, 349.0], [20.8, 349.0], [20.9, 349.0], [21.0, 349.0], [21.1, 349.0], [21.2, 349.0], [21.3, 349.0], [21.4, 349.0], [21.5, 349.0], [21.6, 349.0], [21.7, 349.0], [21.8, 349.0], [21.9, 349.0], [22.0, 351.0], [22.1, 351.0], [22.2, 351.0], [22.3, 351.0], [22.4, 351.0], [22.5, 351.0], [22.6, 351.0], [22.7, 351.0], [22.8, 351.0], [22.9, 351.0], [23.0, 352.0], [23.1, 352.0], [23.2, 352.0], [23.3, 352.0], [23.4, 352.0], [23.5, 352.0], [23.6, 352.0], [23.7, 352.0], [23.8, 352.0], [23.9, 352.0], [24.0, 352.0], [24.1, 352.0], [24.2, 352.0], [24.3, 352.0], [24.4, 352.0], [24.5, 352.0], [24.6, 352.0], [24.7, 352.0], [24.8, 352.0], [24.9, 352.0], [25.0, 361.0], [25.1, 361.0], [25.2, 361.0], [25.3, 361.0], [25.4, 361.0], [25.5, 361.0], [25.6, 361.0], [25.7, 361.0], [25.8, 361.0], [25.9, 361.0], [26.0, 362.0], [26.1, 362.0], [26.2, 362.0], [26.3, 362.0], [26.4, 362.0], [26.5, 362.0], [26.6, 362.0], [26.7, 362.0], [26.8, 362.0], [26.9, 362.0], [27.0, 366.0], [27.1, 366.0], [27.2, 366.0], [27.3, 366.0], [27.4, 366.0], [27.5, 366.0], [27.6, 366.0], [27.7, 366.0], [27.8, 366.0], [27.9, 366.0], [28.0, 367.0], [28.1, 367.0], [28.2, 367.0], [28.3, 367.0], [28.4, 367.0], [28.5, 367.0], [28.6, 367.0], [28.7, 367.0], [28.8, 367.0], [28.9, 367.0], [29.0, 368.0], [29.1, 368.0], [29.2, 368.0], [29.3, 368.0], [29.4, 368.0], [29.5, 368.0], [29.6, 368.0], [29.7, 368.0], [29.8, 368.0], [29.9, 368.0], [30.0, 368.0], [30.1, 368.0], [30.2, 368.0], [30.3, 368.0], [30.4, 368.0], [30.5, 368.0], [30.6, 368.0], [30.7, 368.0], [30.8, 368.0], [30.9, 368.0], [31.0, 369.0], [31.1, 369.0], [31.2, 369.0], [31.3, 369.0], [31.4, 369.0], [31.5, 369.0], [31.6, 369.0], [31.7, 369.0], [31.8, 369.0], [31.9, 369.0], [32.0, 369.0], [32.1, 369.0], [32.2, 369.0], [32.3, 369.0], [32.4, 369.0], [32.5, 369.0], [32.6, 369.0], [32.7, 369.0], [32.8, 369.0], [32.9, 369.0], [33.0, 369.0], [33.1, 369.0], [33.2, 369.0], [33.3, 369.0], [33.4, 369.0], [33.5, 369.0], [33.6, 369.0], [33.7, 369.0], [33.8, 369.0], [33.9, 369.0], [34.0, 371.0], [34.1, 371.0], [34.2, 371.0], [34.3, 371.0], [34.4, 371.0], [34.5, 371.0], [34.6, 371.0], [34.7, 371.0], [34.8, 371.0], [34.9, 371.0], [35.0, 372.0], [35.1, 372.0], [35.2, 372.0], [35.3, 372.0], [35.4, 372.0], [35.5, 372.0], [35.6, 372.0], [35.7, 372.0], [35.8, 372.0], [35.9, 372.0], [36.0, 373.0], [36.1, 373.0], [36.2, 373.0], [36.3, 373.0], [36.4, 373.0], [36.5, 373.0], [36.6, 373.0], [36.7, 373.0], [36.8, 373.0], [36.9, 373.0], [37.0, 373.0], [37.1, 373.0], [37.2, 373.0], [37.3, 373.0], [37.4, 373.0], [37.5, 373.0], [37.6, 373.0], [37.7, 373.0], [37.8, 373.0], [37.9, 373.0], [38.0, 375.0], [38.1, 375.0], [38.2, 375.0], [38.3, 375.0], [38.4, 375.0], [38.5, 375.0], [38.6, 375.0], [38.7, 375.0], [38.8, 375.0], [38.9, 375.0], [39.0, 377.0], [39.1, 377.0], [39.2, 377.0], [39.3, 377.0], [39.4, 377.0], [39.5, 377.0], [39.6, 377.0], [39.7, 377.0], [39.8, 377.0], [39.9, 377.0], [40.0, 378.0], [40.1, 378.0], [40.2, 378.0], [40.3, 378.0], [40.4, 378.0], [40.5, 378.0], [40.6, 378.0], [40.7, 378.0], [40.8, 378.0], [40.9, 378.0], [41.0, 379.0], [41.1, 379.0], [41.2, 379.0], [41.3, 379.0], [41.4, 379.0], [41.5, 379.0], [41.6, 379.0], [41.7, 379.0], [41.8, 379.0], [41.9, 379.0], [42.0, 380.0], [42.1, 380.0], [42.2, 380.0], [42.3, 380.0], [42.4, 380.0], [42.5, 380.0], [42.6, 380.0], [42.7, 380.0], [42.8, 380.0], [42.9, 380.0], [43.0, 382.0], [43.1, 382.0], [43.2, 382.0], [43.3, 382.0], [43.4, 382.0], [43.5, 382.0], [43.6, 382.0], [43.7, 382.0], [43.8, 382.0], [43.9, 382.0], [44.0, 382.0], [44.1, 382.0], [44.2, 382.0], [44.3, 382.0], [44.4, 382.0], [44.5, 382.0], [44.6, 382.0], [44.7, 382.0], [44.8, 382.0], [44.9, 382.0], [45.0, 384.0], [45.1, 384.0], [45.2, 384.0], [45.3, 384.0], [45.4, 384.0], [45.5, 384.0], [45.6, 384.0], [45.7, 384.0], [45.8, 384.0], [45.9, 384.0], [46.0, 384.0], [46.1, 384.0], [46.2, 384.0], [46.3, 384.0], [46.4, 384.0], [46.5, 384.0], [46.6, 384.0], [46.7, 384.0], [46.8, 384.0], [46.9, 384.0], [47.0, 384.0], [47.1, 384.0], [47.2, 384.0], [47.3, 384.0], [47.4, 384.0], [47.5, 384.0], [47.6, 384.0], [47.7, 384.0], [47.8, 384.0], [47.9, 384.0], [48.0, 385.0], [48.1, 385.0], [48.2, 385.0], [48.3, 385.0], [48.4, 385.0], [48.5, 385.0], [48.6, 385.0], [48.7, 385.0], [48.8, 385.0], [48.9, 385.0], [49.0, 386.0], [49.1, 386.0], [49.2, 386.0], [49.3, 386.0], [49.4, 386.0], [49.5, 386.0], [49.6, 386.0], [49.7, 386.0], [49.8, 386.0], [49.9, 386.0], [50.0, 387.0], [50.1, 387.0], [50.2, 387.0], [50.3, 387.0], [50.4, 387.0], [50.5, 387.0], [50.6, 387.0], [50.7, 387.0], [50.8, 387.0], [50.9, 387.0], [51.0, 388.0], [51.1, 388.0], [51.2, 388.0], [51.3, 388.0], [51.4, 388.0], [51.5, 388.0], [51.6, 388.0], [51.7, 388.0], [51.8, 388.0], [51.9, 388.0], [52.0, 389.0], [52.1, 389.0], [52.2, 389.0], [52.3, 389.0], [52.4, 389.0], [52.5, 389.0], [52.6, 389.0], [52.7, 389.0], [52.8, 389.0], [52.9, 389.0], [53.0, 389.0], [53.1, 389.0], [53.2, 389.0], [53.3, 389.0], [53.4, 389.0], [53.5, 389.0], [53.6, 389.0], [53.7, 389.0], [53.8, 389.0], [53.9, 389.0], [54.0, 394.0], [54.1, 394.0], [54.2, 394.0], [54.3, 394.0], [54.4, 394.0], [54.5, 394.0], [54.6, 394.0], [54.7, 394.0], [54.8, 394.0], [54.9, 394.0], [55.0, 394.0], [55.1, 394.0], [55.2, 394.0], [55.3, 394.0], [55.4, 394.0], [55.5, 394.0], [55.6, 394.0], [55.7, 394.0], [55.8, 394.0], [55.9, 394.0], [56.0, 395.0], [56.1, 395.0], [56.2, 395.0], [56.3, 395.0], [56.4, 395.0], [56.5, 395.0], [56.6, 395.0], [56.7, 395.0], [56.8, 395.0], [56.9, 395.0], [57.0, 395.0], [57.1, 395.0], [57.2, 395.0], [57.3, 395.0], [57.4, 395.0], [57.5, 395.0], [57.6, 395.0], [57.7, 395.0], [57.8, 395.0], [57.9, 395.0], [58.0, 407.0], [58.1, 407.0], [58.2, 407.0], [58.3, 407.0], [58.4, 407.0], [58.5, 407.0], [58.6, 407.0], [58.7, 407.0], [58.8, 407.0], [58.9, 407.0], [59.0, 412.0], [59.1, 412.0], [59.2, 412.0], [59.3, 412.0], [59.4, 412.0], [59.5, 412.0], [59.6, 412.0], [59.7, 412.0], [59.8, 412.0], [59.9, 412.0], [60.0, 413.0], [60.1, 413.0], [60.2, 413.0], [60.3, 413.0], [60.4, 413.0], [60.5, 413.0], [60.6, 413.0], [60.7, 413.0], [60.8, 413.0], [60.9, 413.0], [61.0, 415.0], [61.1, 415.0], [61.2, 415.0], [61.3, 415.0], [61.4, 415.0], [61.5, 415.0], [61.6, 415.0], [61.7, 415.0], [61.8, 415.0], [61.9, 415.0], [62.0, 420.0], [62.1, 420.0], [62.2, 420.0], [62.3, 420.0], [62.4, 420.0], [62.5, 420.0], [62.6, 420.0], [62.7, 420.0], [62.8, 420.0], [62.9, 420.0], [63.0, 420.0], [63.1, 420.0], [63.2, 420.0], [63.3, 420.0], [63.4, 420.0], [63.5, 420.0], [63.6, 420.0], [63.7, 420.0], [63.8, 420.0], [63.9, 420.0], [64.0, 422.0], [64.1, 422.0], [64.2, 422.0], [64.3, 422.0], [64.4, 422.0], [64.5, 422.0], [64.6, 422.0], [64.7, 422.0], [64.8, 422.0], [64.9, 422.0], [65.0, 426.0], [65.1, 426.0], [65.2, 426.0], [65.3, 426.0], [65.4, 426.0], [65.5, 426.0], [65.6, 426.0], [65.7, 426.0], [65.8, 426.0], [65.9, 426.0], [66.0, 434.0], [66.1, 434.0], [66.2, 434.0], [66.3, 434.0], [66.4, 434.0], [66.5, 434.0], [66.6, 434.0], [66.7, 434.0], [66.8, 434.0], [66.9, 434.0], [67.0, 436.0], [67.1, 436.0], [67.2, 436.0], [67.3, 436.0], [67.4, 436.0], [67.5, 436.0], [67.6, 436.0], [67.7, 436.0], [67.8, 436.0], [67.9, 436.0], [68.0, 439.0], [68.1, 439.0], [68.2, 439.0], [68.3, 439.0], [68.4, 439.0], [68.5, 439.0], [68.6, 439.0], [68.7, 439.0], [68.8, 439.0], [68.9, 439.0], [69.0, 440.0], [69.1, 440.0], [69.2, 440.0], [69.3, 440.0], [69.4, 440.0], [69.5, 440.0], [69.6, 440.0], [69.7, 440.0], [69.8, 440.0], [69.9, 440.0], [70.0, 442.0], [70.1, 442.0], [70.2, 442.0], [70.3, 442.0], [70.4, 442.0], [70.5, 442.0], [70.6, 442.0], [70.7, 442.0], [70.8, 442.0], [70.9, 442.0], [71.0, 444.0], [71.1, 444.0], [71.2, 444.0], [71.3, 444.0], [71.4, 444.0], [71.5, 444.0], [71.6, 444.0], [71.7, 444.0], [71.8, 444.0], [71.9, 444.0], [72.0, 446.0], [72.1, 446.0], [72.2, 446.0], [72.3, 446.0], [72.4, 446.0], [72.5, 446.0], [72.6, 446.0], [72.7, 446.0], [72.8, 446.0], [72.9, 446.0], [73.0, 451.0], [73.1, 451.0], [73.2, 451.0], [73.3, 451.0], [73.4, 451.0], [73.5, 451.0], [73.6, 451.0], [73.7, 451.0], [73.8, 451.0], [73.9, 451.0], [74.0, 453.0], [74.1, 453.0], [74.2, 453.0], [74.3, 453.0], [74.4, 453.0], [74.5, 453.0], [74.6, 453.0], [74.7, 453.0], [74.8, 453.0], [74.9, 453.0], [75.0, 468.0], [75.1, 468.0], [75.2, 468.0], [75.3, 468.0], [75.4, 468.0], [75.5, 468.0], [75.6, 468.0], [75.7, 468.0], [75.8, 468.0], [75.9, 468.0], [76.0, 481.0], [76.1, 481.0], [76.2, 481.0], [76.3, 481.0], [76.4, 481.0], [76.5, 481.0], [76.6, 481.0], [76.7, 481.0], [76.8, 481.0], [76.9, 481.0], [77.0, 485.0], [77.1, 485.0], [77.2, 485.0], [77.3, 485.0], [77.4, 485.0], [77.5, 485.0], [77.6, 485.0], [77.7, 485.0], [77.8, 485.0], [77.9, 485.0], [78.0, 491.0], [78.1, 491.0], [78.2, 491.0], [78.3, 491.0], [78.4, 491.0], [78.5, 491.0], [78.6, 491.0], [78.7, 491.0], [78.8, 491.0], [78.9, 491.0], [79.0, 505.0], [79.1, 505.0], [79.2, 505.0], [79.3, 505.0], [79.4, 505.0], [79.5, 505.0], [79.6, 505.0], [79.7, 505.0], [79.8, 505.0], [79.9, 505.0], [80.0, 506.0], [80.1, 506.0], [80.2, 506.0], [80.3, 506.0], [80.4, 506.0], [80.5, 506.0], [80.6, 506.0], [80.7, 506.0], [80.8, 506.0], [80.9, 506.0], [81.0, 513.0], [81.1, 513.0], [81.2, 513.0], [81.3, 513.0], [81.4, 513.0], [81.5, 513.0], [81.6, 513.0], [81.7, 513.0], [81.8, 513.0], [81.9, 513.0], [82.0, 520.0], [82.1, 520.0], [82.2, 520.0], [82.3, 520.0], [82.4, 520.0], [82.5, 520.0], [82.6, 520.0], [82.7, 520.0], [82.8, 520.0], [82.9, 520.0], [83.0, 555.0], [83.1, 555.0], [83.2, 555.0], [83.3, 555.0], [83.4, 555.0], [83.5, 555.0], [83.6, 555.0], [83.7, 555.0], [83.8, 555.0], [83.9, 555.0], [84.0, 558.0], [84.1, 558.0], [84.2, 558.0], [84.3, 558.0], [84.4, 558.0], [84.5, 558.0], [84.6, 558.0], [84.7, 558.0], [84.8, 558.0], [84.9, 558.0], [85.0, 597.0], [85.1, 597.0], [85.2, 597.0], [85.3, 597.0], [85.4, 597.0], [85.5, 597.0], [85.6, 597.0], [85.7, 597.0], [85.8, 597.0], [85.9, 597.0], [86.0, 660.0], [86.1, 660.0], [86.2, 660.0], [86.3, 660.0], [86.4, 660.0], [86.5, 660.0], [86.6, 660.0], [86.7, 660.0], [86.8, 660.0], [86.9, 660.0], [87.0, 826.0], [87.1, 826.0], [87.2, 826.0], [87.3, 826.0], [87.4, 826.0], [87.5, 826.0], [87.6, 826.0], [87.7, 826.0], [87.8, 826.0], [87.9, 826.0], [88.0, 977.0], [88.1, 977.0], [88.2, 977.0], [88.3, 977.0], [88.4, 977.0], [88.5, 977.0], [88.6, 977.0], [88.7, 977.0], [88.8, 977.0], [88.9, 977.0], [89.0, 1128.0], [89.1, 1128.0], [89.2, 1128.0], [89.3, 1128.0], [89.4, 1128.0], [89.5, 1128.0], [89.6, 1128.0], [89.7, 1128.0], [89.8, 1128.0], [89.9, 1128.0], [90.0, 1275.0], [90.1, 1275.0], [90.2, 1275.0], [90.3, 1275.0], [90.4, 1275.0], [90.5, 1275.0], [90.6, 1275.0], [90.7, 1275.0], [90.8, 1275.0], [90.9, 1275.0], [91.0, 1402.0], [91.1, 1402.0], [91.2, 1402.0], [91.3, 1402.0], [91.4, 1402.0], [91.5, 1402.0], [91.6, 1402.0], [91.7, 1402.0], [91.8, 1402.0], [91.9, 1402.0], [92.0, 1574.0], [92.1, 1574.0], [92.2, 1574.0], [92.3, 1574.0], [92.4, 1574.0], [92.5, 1574.0], [92.6, 1574.0], [92.7, 1574.0], [92.8, 1574.0], [92.9, 1574.0], [93.0, 1705.0], [93.1, 1705.0], [93.2, 1705.0], [93.3, 1705.0], [93.4, 1705.0], [93.5, 1705.0], [93.6, 1705.0], [93.7, 1705.0], [93.8, 1705.0], [93.9, 1705.0], [94.0, 1903.0], [94.1, 1903.0], [94.2, 1903.0], [94.3, 1903.0], [94.4, 1903.0], [94.5, 1903.0], [94.6, 1903.0], [94.7, 1903.0], [94.8, 1903.0], [94.9, 1903.0], [95.0, 1997.0], [95.1, 1997.0], [95.2, 1997.0], [95.3, 1997.0], [95.4, 1997.0], [95.5, 1997.0], [95.6, 1997.0], [95.7, 1997.0], [95.8, 1997.0], [95.9, 1997.0], [96.0, 2151.0], [96.1, 2151.0], [96.2, 2151.0], [96.3, 2151.0], [96.4, 2151.0], [96.5, 2151.0], [96.6, 2151.0], [96.7, 2151.0], [96.8, 2151.0], [96.9, 2151.0], [97.0, 2315.0], [97.1, 2315.0], [97.2, 2315.0], [97.3, 2315.0], [97.4, 2315.0], [97.5, 2315.0], [97.6, 2315.0], [97.7, 2315.0], [97.8, 2315.0], [97.9, 2315.0], [98.0, 2384.0], [98.1, 2384.0], [98.2, 2384.0], [98.3, 2384.0], [98.4, 2384.0], [98.5, 2384.0], [98.6, 2384.0], [98.7, 2384.0], [98.8, 2384.0], [98.9, 2384.0], [99.0, 2390.0], [99.1, 2390.0], [99.2, 2390.0], [99.3, 2390.0], [99.4, 2390.0], [99.5, 2390.0], [99.6, 2390.0], [99.7, 2390.0], [99.8, 2390.0], [99.9, 2390.0]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 100.0, "title": "Response Time Percentiles"}},
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
        data: {"result": {"minY": 1.0, "minX": 200.0, "maxY": 56.0, "series": [{"data": [[2100.0, 1.0], [2300.0, 3.0], [600.0, 1.0], [800.0, 1.0], [200.0, 2.0], [900.0, 1.0], [1100.0, 1.0], [1200.0, 1.0], [300.0, 56.0], [1400.0, 1.0], [1500.0, 1.0], [400.0, 21.0], [1700.0, 1.0], [1900.0, 2.0], [500.0, 7.0]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 100, "maxX": 2300.0, "title": "Response Time Distribution"}},
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
        data: {"result": {"minY": 8.0, "minX": 0.0, "ticks": [[0, "Requests having \nresponse time <= 500ms"], [1, "Requests having \nresponse time > 500ms and <= 1,500ms"], [2, "Requests having \nresponse time > 1,500ms"], [3, "Requests in error"]], "maxY": 79.0, "series": [{"data": [[0.0, 79.0]], "color": "#9ACD32", "isOverall": false, "label": "Requests having \nresponse time <= 500ms", "isController": false}, {"data": [[1.0, 13.0]], "color": "yellow", "isOverall": false, "label": "Requests having \nresponse time > 500ms and <= 1,500ms", "isController": false}, {"data": [[2.0, 8.0]], "color": "orange", "isOverall": false, "label": "Requests having \nresponse time > 1,500ms", "isController": false}, {"data": [], "color": "#FF6347", "isOverall": false, "label": "Requests in error", "isController": false}], "supportsControllersDiscrimination": false, "maxX": 2.0, "title": "Synthetic Response Times Distribution"}},
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
        data: {"result": {"minY": 4.659999999999999, "minX": 1.79222544E12, "maxY": 4.659999999999999, "series": [{"data": [[1.79222544E12, 4.659999999999999]], "isOverall": false, "label": "Thread Group", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222544E12, "title": "Active Threads Over Time"}},
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
        data: {"result": {"minY": 315.6666666666667, "minX": 1.0, "maxY": 1497.0, "series": [{"data": [[4.0, 480.0], [2.0, 315.6666666666667], [1.0, 361.0], [18.0, 1403.5], [5.0, 1387.0], [12.0, 1497.0], [3.0, 398.10447761194024]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}, {"data": [[4.659999999999999, 563.75]], "isOverall": false, "label": "JM-07-RatingSummary Sampler-Aggregated", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 18.0, "title": "Time VS Threads"}},
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
        data : {"result": {"minY": 676.6666666666666, "minX": 1.79222544E12, "maxY": 1488.3333333333333, "series": [{"data": [[1.79222544E12, 676.6666666666666]], "isOverall": false, "label": "Bytes received per second", "isController": false}, {"data": [[1.79222544E12, 1488.3333333333333]], "isOverall": false, "label": "Bytes sent per second", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222544E12, "title": "Bytes Throughput Over Time"}},
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
        data: {"result": {"minY": 563.75, "minX": 1.79222544E12, "maxY": 563.75, "series": [{"data": [[1.79222544E12, 563.75]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222544E12, "title": "Response Time Over Time"}},
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
        data: {"result": {"minY": 562.2400000000005, "minX": 1.79222544E12, "maxY": 562.2400000000005, "series": [{"data": [[1.79222544E12, 562.2400000000005]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222544E12, "title": "Latencies Over Time"}},
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
        data: {"result": {"minY": 304.0600000000001, "minX": 1.79222544E12, "maxY": 304.0600000000001, "series": [{"data": [[1.79222544E12, 304.0600000000001]], "isOverall": false, "label": "JM-07-RatingSummary Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222544E12, "title": "Connect Time Over Time"}},
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
        data: {"result": {"minY": 288.0, "minX": 1.79222544E12, "maxY": 2390.0, "series": [{"data": [[1.79222544E12, 2390.0]], "isOverall": false, "label": "Max", "isController": false}, {"data": [[1.79222544E12, 288.0]], "isOverall": false, "label": "Min", "isController": false}, {"data": [[1.79222544E12, 1260.3000000000009]], "isOverall": false, "label": "90th percentile", "isController": false}, {"data": [[1.79222544E12, 2389.94]], "isOverall": false, "label": "99th percentile", "isController": false}, {"data": [[1.79222544E12, 386.5]], "isOverall": false, "label": "Median", "isController": false}, {"data": [[1.79222544E12, 1992.2999999999988]], "isOverall": false, "label": "95th percentile", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222544E12, "title": "Response Time Percentiles Over Time (successful requests only)"}},
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
    data: {"result": {"minY": 368.0, "minX": 3.0, "maxY": 1402.0, "series": [{"data": [[17.0, 1402.0], [6.0, 372.0], [3.0, 368.0], [7.0, 382.0]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 17.0, "title": "Response Time Vs Request"}},
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
    data: {"result": {"minY": 368.0, "minX": 3.0, "maxY": 1389.0, "series": [{"data": [[17.0, 1389.0], [6.0, 371.5], [3.0, 368.0], [7.0, 380.5]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 17.0, "title": "Latencies Vs Request"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222544E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222544E12, 1.6666666666666667]], "isOverall": false, "label": "hitsPerSecond", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222544E12, "title": "Hits Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222544E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222544E12, 1.6666666666666667]], "isOverall": false, "label": "200", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222544E12, "title": "Codes Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222544E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222544E12, 1.6666666666666667]], "isOverall": false, "label": "JM-07-RatingSummary Sampler-success", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222544E12, "title": "Transactions Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222544E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222544E12, 1.6666666666666667]], "isOverall": false, "label": "Transaction-success", "isController": false}, {"data": [], "isOverall": false, "label": "Transaction-failure", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222544E12, "title": "Total Transactions Per Second"}},
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

