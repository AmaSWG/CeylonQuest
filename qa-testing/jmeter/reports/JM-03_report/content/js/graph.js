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
        data: {"result": {"minY": 426.0, "minX": 0.0, "maxY": 1539.0, "series": [{"data": [[0.0, 426.0], [0.1, 426.0], [0.2, 426.0], [0.3, 426.0], [0.4, 426.0], [0.5, 426.0], [0.6, 426.0], [0.7, 426.0], [0.8, 426.0], [0.9, 426.0], [1.0, 436.0], [1.1, 436.0], [1.2, 436.0], [1.3, 436.0], [1.4, 436.0], [1.5, 436.0], [1.6, 436.0], [1.7, 436.0], [1.8, 436.0], [1.9, 436.0], [2.0, 436.0], [2.1, 436.0], [2.2, 436.0], [2.3, 436.0], [2.4, 436.0], [2.5, 436.0], [2.6, 436.0], [2.7, 436.0], [2.8, 436.0], [2.9, 436.0], [3.0, 438.0], [3.1, 438.0], [3.2, 438.0], [3.3, 438.0], [3.4, 438.0], [3.5, 438.0], [3.6, 438.0], [3.7, 438.0], [3.8, 438.0], [3.9, 438.0], [4.0, 454.0], [4.1, 454.0], [4.2, 454.0], [4.3, 454.0], [4.4, 454.0], [4.5, 454.0], [4.6, 454.0], [4.7, 454.0], [4.8, 454.0], [4.9, 454.0], [5.0, 455.0], [5.1, 455.0], [5.2, 455.0], [5.3, 455.0], [5.4, 455.0], [5.5, 455.0], [5.6, 455.0], [5.7, 455.0], [5.8, 455.0], [5.9, 455.0], [6.0, 457.0], [6.1, 457.0], [6.2, 457.0], [6.3, 457.0], [6.4, 457.0], [6.5, 457.0], [6.6, 457.0], [6.7, 457.0], [6.8, 457.0], [6.9, 457.0], [7.0, 463.0], [7.1, 463.0], [7.2, 463.0], [7.3, 463.0], [7.4, 463.0], [7.5, 463.0], [7.6, 463.0], [7.7, 463.0], [7.8, 463.0], [7.9, 463.0], [8.0, 465.0], [8.1, 465.0], [8.2, 465.0], [8.3, 465.0], [8.4, 465.0], [8.5, 465.0], [8.6, 465.0], [8.7, 465.0], [8.8, 465.0], [8.9, 465.0], [9.0, 466.0], [9.1, 466.0], [9.2, 466.0], [9.3, 466.0], [9.4, 466.0], [9.5, 466.0], [9.6, 466.0], [9.7, 466.0], [9.8, 466.0], [9.9, 466.0], [10.0, 473.0], [10.1, 473.0], [10.2, 473.0], [10.3, 473.0], [10.4, 473.0], [10.5, 473.0], [10.6, 473.0], [10.7, 473.0], [10.8, 473.0], [10.9, 473.0], [11.0, 477.0], [11.1, 477.0], [11.2, 477.0], [11.3, 477.0], [11.4, 477.0], [11.5, 477.0], [11.6, 477.0], [11.7, 477.0], [11.8, 477.0], [11.9, 477.0], [12.0, 480.0], [12.1, 480.0], [12.2, 480.0], [12.3, 480.0], [12.4, 480.0], [12.5, 480.0], [12.6, 480.0], [12.7, 480.0], [12.8, 480.0], [12.9, 480.0], [13.0, 480.0], [13.1, 480.0], [13.2, 480.0], [13.3, 480.0], [13.4, 480.0], [13.5, 480.0], [13.6, 480.0], [13.7, 480.0], [13.8, 480.0], [13.9, 480.0], [14.0, 485.0], [14.1, 485.0], [14.2, 485.0], [14.3, 485.0], [14.4, 485.0], [14.5, 485.0], [14.6, 485.0], [14.7, 485.0], [14.8, 485.0], [14.9, 485.0], [15.0, 490.0], [15.1, 490.0], [15.2, 490.0], [15.3, 490.0], [15.4, 490.0], [15.5, 490.0], [15.6, 490.0], [15.7, 490.0], [15.8, 490.0], [15.9, 490.0], [16.0, 493.0], [16.1, 493.0], [16.2, 493.0], [16.3, 493.0], [16.4, 493.0], [16.5, 493.0], [16.6, 493.0], [16.7, 493.0], [16.8, 493.0], [16.9, 493.0], [17.0, 494.0], [17.1, 494.0], [17.2, 494.0], [17.3, 494.0], [17.4, 494.0], [17.5, 494.0], [17.6, 494.0], [17.7, 494.0], [17.8, 494.0], [17.9, 494.0], [18.0, 495.0], [18.1, 495.0], [18.2, 495.0], [18.3, 495.0], [18.4, 495.0], [18.5, 495.0], [18.6, 495.0], [18.7, 495.0], [18.8, 495.0], [18.9, 495.0], [19.0, 496.0], [19.1, 496.0], [19.2, 496.0], [19.3, 496.0], [19.4, 496.0], [19.5, 496.0], [19.6, 496.0], [19.7, 496.0], [19.8, 496.0], [19.9, 496.0], [20.0, 501.0], [20.1, 501.0], [20.2, 501.0], [20.3, 501.0], [20.4, 501.0], [20.5, 501.0], [20.6, 501.0], [20.7, 501.0], [20.8, 501.0], [20.9, 501.0], [21.0, 503.0], [21.1, 503.0], [21.2, 503.0], [21.3, 503.0], [21.4, 503.0], [21.5, 503.0], [21.6, 503.0], [21.7, 503.0], [21.8, 503.0], [21.9, 503.0], [22.0, 503.0], [22.1, 503.0], [22.2, 503.0], [22.3, 503.0], [22.4, 503.0], [22.5, 503.0], [22.6, 503.0], [22.7, 503.0], [22.8, 503.0], [22.9, 503.0], [23.0, 505.0], [23.1, 505.0], [23.2, 505.0], [23.3, 505.0], [23.4, 505.0], [23.5, 505.0], [23.6, 505.0], [23.7, 505.0], [23.8, 505.0], [23.9, 505.0], [24.0, 507.0], [24.1, 507.0], [24.2, 507.0], [24.3, 507.0], [24.4, 507.0], [24.5, 507.0], [24.6, 507.0], [24.7, 507.0], [24.8, 507.0], [24.9, 507.0], [25.0, 508.0], [25.1, 508.0], [25.2, 508.0], [25.3, 508.0], [25.4, 508.0], [25.5, 508.0], [25.6, 508.0], [25.7, 508.0], [25.8, 508.0], [25.9, 508.0], [26.0, 510.0], [26.1, 510.0], [26.2, 510.0], [26.3, 510.0], [26.4, 510.0], [26.5, 510.0], [26.6, 510.0], [26.7, 510.0], [26.8, 510.0], [26.9, 510.0], [27.0, 511.0], [27.1, 511.0], [27.2, 511.0], [27.3, 511.0], [27.4, 511.0], [27.5, 511.0], [27.6, 511.0], [27.7, 511.0], [27.8, 511.0], [27.9, 511.0], [28.0, 512.0], [28.1, 512.0], [28.2, 512.0], [28.3, 512.0], [28.4, 512.0], [28.5, 512.0], [28.6, 512.0], [28.7, 512.0], [28.8, 512.0], [28.9, 512.0], [29.0, 513.0], [29.1, 513.0], [29.2, 513.0], [29.3, 513.0], [29.4, 513.0], [29.5, 513.0], [29.6, 513.0], [29.7, 513.0], [29.8, 513.0], [29.9, 513.0], [30.0, 513.0], [30.1, 513.0], [30.2, 513.0], [30.3, 513.0], [30.4, 513.0], [30.5, 513.0], [30.6, 513.0], [30.7, 513.0], [30.8, 513.0], [30.9, 513.0], [31.0, 513.0], [31.1, 513.0], [31.2, 513.0], [31.3, 513.0], [31.4, 513.0], [31.5, 513.0], [31.6, 513.0], [31.7, 513.0], [31.8, 513.0], [31.9, 513.0], [32.0, 514.0], [32.1, 514.0], [32.2, 514.0], [32.3, 514.0], [32.4, 514.0], [32.5, 514.0], [32.6, 514.0], [32.7, 514.0], [32.8, 514.0], [32.9, 514.0], [33.0, 514.0], [33.1, 514.0], [33.2, 514.0], [33.3, 514.0], [33.4, 514.0], [33.5, 514.0], [33.6, 514.0], [33.7, 514.0], [33.8, 514.0], [33.9, 514.0], [34.0, 515.0], [34.1, 515.0], [34.2, 515.0], [34.3, 515.0], [34.4, 515.0], [34.5, 515.0], [34.6, 515.0], [34.7, 515.0], [34.8, 515.0], [34.9, 515.0], [35.0, 519.0], [35.1, 519.0], [35.2, 519.0], [35.3, 519.0], [35.4, 519.0], [35.5, 519.0], [35.6, 519.0], [35.7, 519.0], [35.8, 519.0], [35.9, 519.0], [36.0, 521.0], [36.1, 521.0], [36.2, 521.0], [36.3, 521.0], [36.4, 521.0], [36.5, 521.0], [36.6, 521.0], [36.7, 521.0], [36.8, 521.0], [36.9, 521.0], [37.0, 521.0], [37.1, 521.0], [37.2, 521.0], [37.3, 521.0], [37.4, 521.0], [37.5, 521.0], [37.6, 521.0], [37.7, 521.0], [37.8, 521.0], [37.9, 521.0], [38.0, 522.0], [38.1, 522.0], [38.2, 522.0], [38.3, 522.0], [38.4, 522.0], [38.5, 522.0], [38.6, 522.0], [38.7, 522.0], [38.8, 522.0], [38.9, 522.0], [39.0, 524.0], [39.1, 524.0], [39.2, 524.0], [39.3, 524.0], [39.4, 524.0], [39.5, 524.0], [39.6, 524.0], [39.7, 524.0], [39.8, 524.0], [39.9, 524.0], [40.0, 524.0], [40.1, 524.0], [40.2, 524.0], [40.3, 524.0], [40.4, 524.0], [40.5, 524.0], [40.6, 524.0], [40.7, 524.0], [40.8, 524.0], [40.9, 524.0], [41.0, 524.0], [41.1, 524.0], [41.2, 524.0], [41.3, 524.0], [41.4, 524.0], [41.5, 524.0], [41.6, 524.0], [41.7, 524.0], [41.8, 524.0], [41.9, 524.0], [42.0, 524.0], [42.1, 524.0], [42.2, 524.0], [42.3, 524.0], [42.4, 524.0], [42.5, 524.0], [42.6, 524.0], [42.7, 524.0], [42.8, 524.0], [42.9, 524.0], [43.0, 525.0], [43.1, 525.0], [43.2, 525.0], [43.3, 525.0], [43.4, 525.0], [43.5, 525.0], [43.6, 525.0], [43.7, 525.0], [43.8, 525.0], [43.9, 525.0], [44.0, 527.0], [44.1, 527.0], [44.2, 527.0], [44.3, 527.0], [44.4, 527.0], [44.5, 527.0], [44.6, 527.0], [44.7, 527.0], [44.8, 527.0], [44.9, 527.0], [45.0, 528.0], [45.1, 528.0], [45.2, 528.0], [45.3, 528.0], [45.4, 528.0], [45.5, 528.0], [45.6, 528.0], [45.7, 528.0], [45.8, 528.0], [45.9, 528.0], [46.0, 529.0], [46.1, 529.0], [46.2, 529.0], [46.3, 529.0], [46.4, 529.0], [46.5, 529.0], [46.6, 529.0], [46.7, 529.0], [46.8, 529.0], [46.9, 529.0], [47.0, 530.0], [47.1, 530.0], [47.2, 530.0], [47.3, 530.0], [47.4, 530.0], [47.5, 530.0], [47.6, 530.0], [47.7, 530.0], [47.8, 530.0], [47.9, 530.0], [48.0, 531.0], [48.1, 531.0], [48.2, 531.0], [48.3, 531.0], [48.4, 531.0], [48.5, 531.0], [48.6, 531.0], [48.7, 531.0], [48.8, 531.0], [48.9, 531.0], [49.0, 533.0], [49.1, 533.0], [49.2, 533.0], [49.3, 533.0], [49.4, 533.0], [49.5, 533.0], [49.6, 533.0], [49.7, 533.0], [49.8, 533.0], [49.9, 533.0], [50.0, 533.0], [50.1, 533.0], [50.2, 533.0], [50.3, 533.0], [50.4, 533.0], [50.5, 533.0], [50.6, 533.0], [50.7, 533.0], [50.8, 533.0], [50.9, 533.0], [51.0, 533.0], [51.1, 533.0], [51.2, 533.0], [51.3, 533.0], [51.4, 533.0], [51.5, 533.0], [51.6, 533.0], [51.7, 533.0], [51.8, 533.0], [51.9, 533.0], [52.0, 534.0], [52.1, 534.0], [52.2, 534.0], [52.3, 534.0], [52.4, 534.0], [52.5, 534.0], [52.6, 534.0], [52.7, 534.0], [52.8, 534.0], [52.9, 534.0], [53.0, 535.0], [53.1, 535.0], [53.2, 535.0], [53.3, 535.0], [53.4, 535.0], [53.5, 535.0], [53.6, 535.0], [53.7, 535.0], [53.8, 535.0], [53.9, 535.0], [54.0, 535.0], [54.1, 535.0], [54.2, 535.0], [54.3, 535.0], [54.4, 535.0], [54.5, 535.0], [54.6, 535.0], [54.7, 535.0], [54.8, 535.0], [54.9, 535.0], [55.0, 536.0], [55.1, 536.0], [55.2, 536.0], [55.3, 536.0], [55.4, 536.0], [55.5, 536.0], [55.6, 536.0], [55.7, 536.0], [55.8, 536.0], [55.9, 536.0], [56.0, 537.0], [56.1, 537.0], [56.2, 537.0], [56.3, 537.0], [56.4, 537.0], [56.5, 537.0], [56.6, 537.0], [56.7, 537.0], [56.8, 537.0], [56.9, 537.0], [57.0, 538.0], [57.1, 538.0], [57.2, 538.0], [57.3, 538.0], [57.4, 538.0], [57.5, 538.0], [57.6, 538.0], [57.7, 538.0], [57.8, 538.0], [57.9, 538.0], [58.0, 539.0], [58.1, 539.0], [58.2, 539.0], [58.3, 539.0], [58.4, 539.0], [58.5, 539.0], [58.6, 539.0], [58.7, 539.0], [58.8, 539.0], [58.9, 539.0], [59.0, 541.0], [59.1, 541.0], [59.2, 541.0], [59.3, 541.0], [59.4, 541.0], [59.5, 541.0], [59.6, 541.0], [59.7, 541.0], [59.8, 541.0], [59.9, 541.0], [60.0, 550.0], [60.1, 550.0], [60.2, 550.0], [60.3, 550.0], [60.4, 550.0], [60.5, 550.0], [60.6, 550.0], [60.7, 550.0], [60.8, 550.0], [60.9, 550.0], [61.0, 553.0], [61.1, 553.0], [61.2, 553.0], [61.3, 553.0], [61.4, 553.0], [61.5, 553.0], [61.6, 553.0], [61.7, 553.0], [61.8, 553.0], [61.9, 553.0], [62.0, 554.0], [62.1, 554.0], [62.2, 554.0], [62.3, 554.0], [62.4, 554.0], [62.5, 554.0], [62.6, 554.0], [62.7, 554.0], [62.8, 554.0], [62.9, 554.0], [63.0, 557.0], [63.1, 557.0], [63.2, 557.0], [63.3, 557.0], [63.4, 557.0], [63.5, 557.0], [63.6, 557.0], [63.7, 557.0], [63.8, 557.0], [63.9, 557.0], [64.0, 558.0], [64.1, 558.0], [64.2, 558.0], [64.3, 558.0], [64.4, 558.0], [64.5, 558.0], [64.6, 558.0], [64.7, 558.0], [64.8, 558.0], [64.9, 558.0], [65.0, 559.0], [65.1, 559.0], [65.2, 559.0], [65.3, 559.0], [65.4, 559.0], [65.5, 559.0], [65.6, 559.0], [65.7, 559.0], [65.8, 559.0], [65.9, 559.0], [66.0, 560.0], [66.1, 560.0], [66.2, 560.0], [66.3, 560.0], [66.4, 560.0], [66.5, 560.0], [66.6, 560.0], [66.7, 560.0], [66.8, 560.0], [66.9, 560.0], [67.0, 561.0], [67.1, 561.0], [67.2, 561.0], [67.3, 561.0], [67.4, 561.0], [67.5, 561.0], [67.6, 561.0], [67.7, 561.0], [67.8, 561.0], [67.9, 561.0], [68.0, 567.0], [68.1, 567.0], [68.2, 567.0], [68.3, 567.0], [68.4, 567.0], [68.5, 567.0], [68.6, 567.0], [68.7, 567.0], [68.8, 567.0], [68.9, 567.0], [69.0, 567.0], [69.1, 567.0], [69.2, 567.0], [69.3, 567.0], [69.4, 567.0], [69.5, 567.0], [69.6, 567.0], [69.7, 567.0], [69.8, 567.0], [69.9, 567.0], [70.0, 569.0], [70.1, 569.0], [70.2, 569.0], [70.3, 569.0], [70.4, 569.0], [70.5, 569.0], [70.6, 569.0], [70.7, 569.0], [70.8, 569.0], [70.9, 569.0], [71.0, 573.0], [71.1, 573.0], [71.2, 573.0], [71.3, 573.0], [71.4, 573.0], [71.5, 573.0], [71.6, 573.0], [71.7, 573.0], [71.8, 573.0], [71.9, 573.0], [72.0, 573.0], [72.1, 573.0], [72.2, 573.0], [72.3, 573.0], [72.4, 573.0], [72.5, 573.0], [72.6, 573.0], [72.7, 573.0], [72.8, 573.0], [72.9, 573.0], [73.0, 574.0], [73.1, 574.0], [73.2, 574.0], [73.3, 574.0], [73.4, 574.0], [73.5, 574.0], [73.6, 574.0], [73.7, 574.0], [73.8, 574.0], [73.9, 574.0], [74.0, 576.0], [74.1, 576.0], [74.2, 576.0], [74.3, 576.0], [74.4, 576.0], [74.5, 576.0], [74.6, 576.0], [74.7, 576.0], [74.8, 576.0], [74.9, 576.0], [75.0, 579.0], [75.1, 579.0], [75.2, 579.0], [75.3, 579.0], [75.4, 579.0], [75.5, 579.0], [75.6, 579.0], [75.7, 579.0], [75.8, 579.0], [75.9, 579.0], [76.0, 584.0], [76.1, 584.0], [76.2, 584.0], [76.3, 584.0], [76.4, 584.0], [76.5, 584.0], [76.6, 584.0], [76.7, 584.0], [76.8, 584.0], [76.9, 584.0], [77.0, 585.0], [77.1, 585.0], [77.2, 585.0], [77.3, 585.0], [77.4, 585.0], [77.5, 585.0], [77.6, 585.0], [77.7, 585.0], [77.8, 585.0], [77.9, 585.0], [78.0, 596.0], [78.1, 596.0], [78.2, 596.0], [78.3, 596.0], [78.4, 596.0], [78.5, 596.0], [78.6, 596.0], [78.7, 596.0], [78.8, 596.0], [78.9, 596.0], [79.0, 615.0], [79.1, 615.0], [79.2, 615.0], [79.3, 615.0], [79.4, 615.0], [79.5, 615.0], [79.6, 615.0], [79.7, 615.0], [79.8, 615.0], [79.9, 615.0], [80.0, 615.0], [80.1, 615.0], [80.2, 615.0], [80.3, 615.0], [80.4, 615.0], [80.5, 615.0], [80.6, 615.0], [80.7, 615.0], [80.8, 615.0], [80.9, 615.0], [81.0, 619.0], [81.1, 619.0], [81.2, 619.0], [81.3, 619.0], [81.4, 619.0], [81.5, 619.0], [81.6, 619.0], [81.7, 619.0], [81.8, 619.0], [81.9, 619.0], [82.0, 622.0], [82.1, 622.0], [82.2, 622.0], [82.3, 622.0], [82.4, 622.0], [82.5, 622.0], [82.6, 622.0], [82.7, 622.0], [82.8, 622.0], [82.9, 622.0], [83.0, 623.0], [83.1, 623.0], [83.2, 623.0], [83.3, 623.0], [83.4, 623.0], [83.5, 623.0], [83.6, 623.0], [83.7, 623.0], [83.8, 623.0], [83.9, 623.0], [84.0, 627.0], [84.1, 627.0], [84.2, 627.0], [84.3, 627.0], [84.4, 627.0], [84.5, 627.0], [84.6, 627.0], [84.7, 627.0], [84.8, 627.0], [84.9, 627.0], [85.0, 636.0], [85.1, 636.0], [85.2, 636.0], [85.3, 636.0], [85.4, 636.0], [85.5, 636.0], [85.6, 636.0], [85.7, 636.0], [85.8, 636.0], [85.9, 636.0], [86.0, 669.0], [86.1, 669.0], [86.2, 669.0], [86.3, 669.0], [86.4, 669.0], [86.5, 669.0], [86.6, 669.0], [86.7, 669.0], [86.8, 669.0], [86.9, 669.0], [87.0, 672.0], [87.1, 672.0], [87.2, 672.0], [87.3, 672.0], [87.4, 672.0], [87.5, 672.0], [87.6, 672.0], [87.7, 672.0], [87.8, 672.0], [87.9, 672.0], [88.0, 673.0], [88.1, 673.0], [88.2, 673.0], [88.3, 673.0], [88.4, 673.0], [88.5, 673.0], [88.6, 673.0], [88.7, 673.0], [88.8, 673.0], [88.9, 673.0], [89.0, 772.0], [89.1, 772.0], [89.2, 772.0], [89.3, 772.0], [89.4, 772.0], [89.5, 772.0], [89.6, 772.0], [89.7, 772.0], [89.8, 772.0], [89.9, 772.0], [90.0, 873.0], [90.1, 873.0], [90.2, 873.0], [90.3, 873.0], [90.4, 873.0], [90.5, 873.0], [90.6, 873.0], [90.7, 873.0], [90.8, 873.0], [90.9, 873.0], [91.0, 971.0], [91.1, 971.0], [91.2, 971.0], [91.3, 971.0], [91.4, 971.0], [91.5, 971.0], [91.6, 971.0], [91.7, 971.0], [91.8, 971.0], [91.9, 971.0], [92.0, 1072.0], [92.1, 1072.0], [92.2, 1072.0], [92.3, 1072.0], [92.4, 1072.0], [92.5, 1072.0], [92.6, 1072.0], [92.7, 1072.0], [92.8, 1072.0], [92.9, 1072.0], [93.0, 1169.0], [93.1, 1169.0], [93.2, 1169.0], [93.3, 1169.0], [93.4, 1169.0], [93.5, 1169.0], [93.6, 1169.0], [93.7, 1169.0], [93.8, 1169.0], [93.9, 1169.0], [94.0, 1271.0], [94.1, 1271.0], [94.2, 1271.0], [94.3, 1271.0], [94.4, 1271.0], [94.5, 1271.0], [94.6, 1271.0], [94.7, 1271.0], [94.8, 1271.0], [94.9, 1271.0], [95.0, 1369.0], [95.1, 1369.0], [95.2, 1369.0], [95.3, 1369.0], [95.4, 1369.0], [95.5, 1369.0], [95.6, 1369.0], [95.7, 1369.0], [95.8, 1369.0], [95.9, 1369.0], [96.0, 1468.0], [96.1, 1468.0], [96.2, 1468.0], [96.3, 1468.0], [96.4, 1468.0], [96.5, 1468.0], [96.6, 1468.0], [96.7, 1468.0], [96.8, 1468.0], [96.9, 1468.0], [97.0, 1539.0], [97.1, 1539.0], [97.2, 1539.0], [97.3, 1539.0], [97.4, 1539.0], [97.5, 1539.0], [97.6, 1539.0], [97.7, 1539.0], [97.8, 1539.0], [97.9, 1539.0], [98.0, 1539.0], [98.1, 1539.0], [98.2, 1539.0], [98.3, 1539.0], [98.4, 1539.0], [98.5, 1539.0], [98.6, 1539.0], [98.7, 1539.0], [98.8, 1539.0], [98.9, 1539.0], [99.0, 1539.0], [99.1, 1539.0], [99.2, 1539.0], [99.3, 1539.0], [99.4, 1539.0], [99.5, 1539.0], [99.6, 1539.0], [99.7, 1539.0], [99.8, 1539.0], [99.9, 1539.0]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 100.0, "title": "Response Time Percentiles"}},
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
        data: {"result": {"minY": 1.0, "minX": 400.0, "maxY": 59.0, "series": [{"data": [[1100.0, 1.0], [600.0, 10.0], [1200.0, 1.0], [1300.0, 1.0], [700.0, 1.0], [1400.0, 1.0], [1500.0, 3.0], [800.0, 1.0], [400.0, 20.0], [900.0, 1.0], [1000.0, 1.0], [500.0, 59.0]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 100, "maxX": 1500.0, "title": "Response Time Distribution"}},
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
        data: {"result": {"minY": 3.0, "minX": 0.0, "ticks": [[0, "Requests having \nresponse time <= 500ms"], [1, "Requests having \nresponse time > 500ms and <= 1,500ms"], [2, "Requests having \nresponse time > 1,500ms"], [3, "Requests in error"]], "maxY": 77.0, "series": [{"data": [[0.0, 20.0]], "color": "#9ACD32", "isOverall": false, "label": "Requests having \nresponse time <= 500ms", "isController": false}, {"data": [[1.0, 77.0]], "color": "yellow", "isOverall": false, "label": "Requests having \nresponse time > 500ms and <= 1,500ms", "isController": false}, {"data": [[2.0, 3.0]], "color": "orange", "isOverall": false, "label": "Requests having \nresponse time > 1,500ms", "isController": false}, {"data": [], "color": "#FF6347", "isOverall": false, "label": "Requests in error", "isController": false}], "supportsControllersDiscrimination": false, "maxX": 2.0, "title": "Synthetic Response Times Distribution"}},
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
        data: {"result": {"minY": 7.339999999999998, "minX": 1.79222532E12, "maxY": 7.339999999999998, "series": [{"data": [[1.79222532E12, 7.339999999999998]], "isOverall": false, "label": "Thread Group", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222532E12, "title": "Active Threads Over Time"}},
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
        data: {"result": {"minY": 471.46153846153845, "minX": 2.0, "maxY": 1187.8333333333335, "series": [{"data": [[4.0, 535.0], [2.0, 505.5], [18.0, 1187.8333333333335], [5.0, 471.46153846153845], [6.0, 533.0000000000001], [3.0, 533.0], [7.0, 575.9374999999999]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}, {"data": [[7.339999999999998, 609.9200000000002]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler-Aggregated", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 18.0, "title": "Time VS Threads"}},
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
        data : {"result": {"minY": 1331.6666666666667, "minX": 1.79222532E12, "maxY": 1941.6666666666667, "series": [{"data": [[1.79222532E12, 1941.6666666666667]], "isOverall": false, "label": "Bytes received per second", "isController": false}, {"data": [[1.79222532E12, 1331.6666666666667]], "isOverall": false, "label": "Bytes sent per second", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222532E12, "title": "Bytes Throughput Over Time"}},
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
        data: {"result": {"minY": 609.9200000000002, "minX": 1.79222532E12, "maxY": 609.9200000000002, "series": [{"data": [[1.79222532E12, 609.9200000000002]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222532E12, "title": "Response Time Over Time"}},
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
        data: {"result": {"minY": 608.1600000000004, "minX": 1.79222532E12, "maxY": 608.1600000000004, "series": [{"data": [[1.79222532E12, 608.1600000000004]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222532E12, "title": "Latencies Over Time"}},
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
        data: {"result": {"minY": 459.3599999999999, "minX": 1.79222532E12, "maxY": 459.3599999999999, "series": [{"data": [[1.79222532E12, 459.3599999999999]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222532E12, "title": "Connect Time Over Time"}},
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
        data: {"result": {"minY": 426.0, "minX": 1.79222532E12, "maxY": 1539.0, "series": [{"data": [[1.79222532E12, 1539.0]], "isOverall": false, "label": "Max", "isController": false}, {"data": [[1.79222532E12, 426.0]], "isOverall": false, "label": "Min", "isController": false}, {"data": [[1.79222532E12, 862.9000000000005]], "isOverall": false, "label": "90th percentile", "isController": false}, {"data": [[1.79222532E12, 1539.0]], "isOverall": false, "label": "99th percentile", "isController": false}, {"data": [[1.79222532E12, 533.0]], "isOverall": false, "label": "Median", "isController": false}, {"data": [[1.79222532E12, 1364.099999999999]], "isOverall": false, "label": "95th percentile", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222532E12, "title": "Response Time Percentiles Over Time (successful requests only)"}},
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
    data: {"result": {"minY": 505.5, "minX": 2.0, "maxY": 873.0, "series": [{"data": [[2.0, 505.5], [9.0, 512.5], [19.0, 873.0], [10.0, 528.5], [11.0, 553.0]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 19.0, "title": "Response Time Vs Request"}},
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
    data: {"result": {"minY": 505.5, "minX": 2.0, "maxY": 859.0, "series": [{"data": [[2.0, 505.5], [9.0, 512.5], [19.0, 859.0], [10.0, 528.5], [11.0, 553.0]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 19.0, "title": "Latencies Vs Request"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222532E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222532E12, 1.6666666666666667]], "isOverall": false, "label": "hitsPerSecond", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222532E12, "title": "Hits Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222532E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222532E12, 1.6666666666666667]], "isOverall": false, "label": "200", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.79222532E12, "title": "Codes Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222532E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222532E12, 1.6666666666666667]], "isOverall": false, "label": "JM-03-UnreadBadgePolling Sampler-success", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222532E12, "title": "Transactions Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.79222532E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.79222532E12, 1.6666666666666667]], "isOverall": false, "label": "Transaction-success", "isController": false}, {"data": [], "isOverall": false, "label": "Transaction-failure", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.79222532E12, "title": "Total Transactions Per Second"}},
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

