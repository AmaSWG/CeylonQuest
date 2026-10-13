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
        data: {"result": {"minY": 501.0, "minX": 0.0, "maxY": 1778.0, "series": [{"data": [[0.0, 501.0], [0.1, 501.0], [0.2, 501.0], [0.3, 501.0], [0.4, 501.0], [0.5, 501.0], [0.6, 501.0], [0.7, 501.0], [0.8, 501.0], [0.9, 501.0], [1.0, 502.0], [1.1, 502.0], [1.2, 502.0], [1.3, 502.0], [1.4, 502.0], [1.5, 502.0], [1.6, 502.0], [1.7, 502.0], [1.8, 502.0], [1.9, 502.0], [2.0, 502.0], [2.1, 502.0], [2.2, 502.0], [2.3, 502.0], [2.4, 502.0], [2.5, 502.0], [2.6, 502.0], [2.7, 502.0], [2.8, 502.0], [2.9, 502.0], [3.0, 504.0], [3.1, 504.0], [3.2, 504.0], [3.3, 504.0], [3.4, 504.0], [3.5, 504.0], [3.6, 504.0], [3.7, 504.0], [3.8, 504.0], [3.9, 504.0], [4.0, 507.0], [4.1, 507.0], [4.2, 507.0], [4.3, 507.0], [4.4, 507.0], [4.5, 507.0], [4.6, 507.0], [4.7, 507.0], [4.8, 507.0], [4.9, 507.0], [5.0, 509.0], [5.1, 509.0], [5.2, 509.0], [5.3, 509.0], [5.4, 509.0], [5.5, 509.0], [5.6, 509.0], [5.7, 509.0], [5.8, 509.0], [5.9, 509.0], [6.0, 520.0], [6.1, 520.0], [6.2, 520.0], [6.3, 520.0], [6.4, 520.0], [6.5, 520.0], [6.6, 520.0], [6.7, 520.0], [6.8, 520.0], [6.9, 520.0], [7.0, 521.0], [7.1, 521.0], [7.2, 521.0], [7.3, 521.0], [7.4, 521.0], [7.5, 521.0], [7.6, 521.0], [7.7, 521.0], [7.8, 521.0], [7.9, 521.0], [8.0, 526.0], [8.1, 526.0], [8.2, 526.0], [8.3, 526.0], [8.4, 526.0], [8.5, 526.0], [8.6, 526.0], [8.7, 526.0], [8.8, 526.0], [8.9, 526.0], [9.0, 527.0], [9.1, 527.0], [9.2, 527.0], [9.3, 527.0], [9.4, 527.0], [9.5, 527.0], [9.6, 527.0], [9.7, 527.0], [9.8, 527.0], [9.9, 527.0], [10.0, 533.0], [10.1, 533.0], [10.2, 533.0], [10.3, 533.0], [10.4, 533.0], [10.5, 533.0], [10.6, 533.0], [10.7, 533.0], [10.8, 533.0], [10.9, 533.0], [11.0, 538.0], [11.1, 538.0], [11.2, 538.0], [11.3, 538.0], [11.4, 538.0], [11.5, 538.0], [11.6, 538.0], [11.7, 538.0], [11.8, 538.0], [11.9, 538.0], [12.0, 544.0], [12.1, 544.0], [12.2, 544.0], [12.3, 544.0], [12.4, 544.0], [12.5, 544.0], [12.6, 544.0], [12.7, 544.0], [12.8, 544.0], [12.9, 544.0], [13.0, 546.0], [13.1, 546.0], [13.2, 546.0], [13.3, 546.0], [13.4, 546.0], [13.5, 546.0], [13.6, 546.0], [13.7, 546.0], [13.8, 546.0], [13.9, 546.0], [14.0, 547.0], [14.1, 547.0], [14.2, 547.0], [14.3, 547.0], [14.4, 547.0], [14.5, 547.0], [14.6, 547.0], [14.7, 547.0], [14.8, 547.0], [14.9, 547.0], [15.0, 547.0], [15.1, 547.0], [15.2, 547.0], [15.3, 547.0], [15.4, 547.0], [15.5, 547.0], [15.6, 547.0], [15.7, 547.0], [15.8, 547.0], [15.9, 547.0], [16.0, 548.0], [16.1, 548.0], [16.2, 548.0], [16.3, 548.0], [16.4, 548.0], [16.5, 548.0], [16.6, 548.0], [16.7, 548.0], [16.8, 548.0], [16.9, 548.0], [17.0, 549.0], [17.1, 549.0], [17.2, 549.0], [17.3, 549.0], [17.4, 549.0], [17.5, 549.0], [17.6, 549.0], [17.7, 549.0], [17.8, 549.0], [17.9, 549.0], [18.0, 556.0], [18.1, 556.0], [18.2, 556.0], [18.3, 556.0], [18.4, 556.0], [18.5, 556.0], [18.6, 556.0], [18.7, 556.0], [18.8, 556.0], [18.9, 556.0], [19.0, 556.0], [19.1, 556.0], [19.2, 556.0], [19.3, 556.0], [19.4, 556.0], [19.5, 556.0], [19.6, 556.0], [19.7, 556.0], [19.8, 556.0], [19.9, 556.0], [20.0, 558.0], [20.1, 558.0], [20.2, 558.0], [20.3, 558.0], [20.4, 558.0], [20.5, 558.0], [20.6, 558.0], [20.7, 558.0], [20.8, 558.0], [20.9, 558.0], [21.0, 560.0], [21.1, 560.0], [21.2, 560.0], [21.3, 560.0], [21.4, 560.0], [21.5, 560.0], [21.6, 560.0], [21.7, 560.0], [21.8, 560.0], [21.9, 560.0], [22.0, 560.0], [22.1, 560.0], [22.2, 560.0], [22.3, 560.0], [22.4, 560.0], [22.5, 560.0], [22.6, 560.0], [22.7, 560.0], [22.8, 560.0], [22.9, 560.0], [23.0, 561.0], [23.1, 561.0], [23.2, 561.0], [23.3, 561.0], [23.4, 561.0], [23.5, 561.0], [23.6, 561.0], [23.7, 561.0], [23.8, 561.0], [23.9, 561.0], [24.0, 563.0], [24.1, 563.0], [24.2, 563.0], [24.3, 563.0], [24.4, 563.0], [24.5, 563.0], [24.6, 563.0], [24.7, 563.0], [24.8, 563.0], [24.9, 563.0], [25.0, 565.0], [25.1, 565.0], [25.2, 565.0], [25.3, 565.0], [25.4, 565.0], [25.5, 565.0], [25.6, 565.0], [25.7, 565.0], [25.8, 565.0], [25.9, 565.0], [26.0, 566.0], [26.1, 566.0], [26.2, 566.0], [26.3, 566.0], [26.4, 566.0], [26.5, 566.0], [26.6, 566.0], [26.7, 566.0], [26.8, 566.0], [26.9, 566.0], [27.0, 567.0], [27.1, 567.0], [27.2, 567.0], [27.3, 567.0], [27.4, 567.0], [27.5, 567.0], [27.6, 567.0], [27.7, 567.0], [27.8, 567.0], [27.9, 567.0], [28.0, 567.0], [28.1, 567.0], [28.2, 567.0], [28.3, 567.0], [28.4, 567.0], [28.5, 567.0], [28.6, 567.0], [28.7, 567.0], [28.8, 567.0], [28.9, 567.0], [29.0, 567.0], [29.1, 567.0], [29.2, 567.0], [29.3, 567.0], [29.4, 567.0], [29.5, 567.0], [29.6, 567.0], [29.7, 567.0], [29.8, 567.0], [29.9, 567.0], [30.0, 569.0], [30.1, 569.0], [30.2, 569.0], [30.3, 569.0], [30.4, 569.0], [30.5, 569.0], [30.6, 569.0], [30.7, 569.0], [30.8, 569.0], [30.9, 569.0], [31.0, 571.0], [31.1, 571.0], [31.2, 571.0], [31.3, 571.0], [31.4, 571.0], [31.5, 571.0], [31.6, 571.0], [31.7, 571.0], [31.8, 571.0], [31.9, 571.0], [32.0, 573.0], [32.1, 573.0], [32.2, 573.0], [32.3, 573.0], [32.4, 573.0], [32.5, 573.0], [32.6, 573.0], [32.7, 573.0], [32.8, 573.0], [32.9, 573.0], [33.0, 575.0], [33.1, 575.0], [33.2, 575.0], [33.3, 575.0], [33.4, 575.0], [33.5, 575.0], [33.6, 575.0], [33.7, 575.0], [33.8, 575.0], [33.9, 575.0], [34.0, 578.0], [34.1, 578.0], [34.2, 578.0], [34.3, 578.0], [34.4, 578.0], [34.5, 578.0], [34.6, 578.0], [34.7, 578.0], [34.8, 578.0], [34.9, 578.0], [35.0, 584.0], [35.1, 584.0], [35.2, 584.0], [35.3, 584.0], [35.4, 584.0], [35.5, 584.0], [35.6, 584.0], [35.7, 584.0], [35.8, 584.0], [35.9, 584.0], [36.0, 586.0], [36.1, 586.0], [36.2, 586.0], [36.3, 586.0], [36.4, 586.0], [36.5, 586.0], [36.6, 586.0], [36.7, 586.0], [36.8, 586.0], [36.9, 586.0], [37.0, 588.0], [37.1, 588.0], [37.2, 588.0], [37.3, 588.0], [37.4, 588.0], [37.5, 588.0], [37.6, 588.0], [37.7, 588.0], [37.8, 588.0], [37.9, 588.0], [38.0, 589.0], [38.1, 589.0], [38.2, 589.0], [38.3, 589.0], [38.4, 589.0], [38.5, 589.0], [38.6, 589.0], [38.7, 589.0], [38.8, 589.0], [38.9, 589.0], [39.0, 590.0], [39.1, 590.0], [39.2, 590.0], [39.3, 590.0], [39.4, 590.0], [39.5, 590.0], [39.6, 590.0], [39.7, 590.0], [39.8, 590.0], [39.9, 590.0], [40.0, 591.0], [40.1, 591.0], [40.2, 591.0], [40.3, 591.0], [40.4, 591.0], [40.5, 591.0], [40.6, 591.0], [40.7, 591.0], [40.8, 591.0], [40.9, 591.0], [41.0, 594.0], [41.1, 594.0], [41.2, 594.0], [41.3, 594.0], [41.4, 594.0], [41.5, 594.0], [41.6, 594.0], [41.7, 594.0], [41.8, 594.0], [41.9, 594.0], [42.0, 594.0], [42.1, 594.0], [42.2, 594.0], [42.3, 594.0], [42.4, 594.0], [42.5, 594.0], [42.6, 594.0], [42.7, 594.0], [42.8, 594.0], [42.9, 594.0], [43.0, 595.0], [43.1, 595.0], [43.2, 595.0], [43.3, 595.0], [43.4, 595.0], [43.5, 595.0], [43.6, 595.0], [43.7, 595.0], [43.8, 595.0], [43.9, 595.0], [44.0, 597.0], [44.1, 597.0], [44.2, 597.0], [44.3, 597.0], [44.4, 597.0], [44.5, 597.0], [44.6, 597.0], [44.7, 597.0], [44.8, 597.0], [44.9, 597.0], [45.0, 600.0], [45.1, 600.0], [45.2, 600.0], [45.3, 600.0], [45.4, 600.0], [45.5, 600.0], [45.6, 600.0], [45.7, 600.0], [45.8, 600.0], [45.9, 600.0], [46.0, 603.0], [46.1, 603.0], [46.2, 603.0], [46.3, 603.0], [46.4, 603.0], [46.5, 603.0], [46.6, 603.0], [46.7, 603.0], [46.8, 603.0], [46.9, 603.0], [47.0, 605.0], [47.1, 605.0], [47.2, 605.0], [47.3, 605.0], [47.4, 605.0], [47.5, 605.0], [47.6, 605.0], [47.7, 605.0], [47.8, 605.0], [47.9, 605.0], [48.0, 608.0], [48.1, 608.0], [48.2, 608.0], [48.3, 608.0], [48.4, 608.0], [48.5, 608.0], [48.6, 608.0], [48.7, 608.0], [48.8, 608.0], [48.9, 608.0], [49.0, 609.0], [49.1, 609.0], [49.2, 609.0], [49.3, 609.0], [49.4, 609.0], [49.5, 609.0], [49.6, 609.0], [49.7, 609.0], [49.8, 609.0], [49.9, 609.0], [50.0, 613.0], [50.1, 613.0], [50.2, 613.0], [50.3, 613.0], [50.4, 613.0], [50.5, 613.0], [50.6, 613.0], [50.7, 613.0], [50.8, 613.0], [50.9, 613.0], [51.0, 616.0], [51.1, 616.0], [51.2, 616.0], [51.3, 616.0], [51.4, 616.0], [51.5, 616.0], [51.6, 616.0], [51.7, 616.0], [51.8, 616.0], [51.9, 616.0], [52.0, 616.0], [52.1, 616.0], [52.2, 616.0], [52.3, 616.0], [52.4, 616.0], [52.5, 616.0], [52.6, 616.0], [52.7, 616.0], [52.8, 616.0], [52.9, 616.0], [53.0, 616.0], [53.1, 616.0], [53.2, 616.0], [53.3, 616.0], [53.4, 616.0], [53.5, 616.0], [53.6, 616.0], [53.7, 616.0], [53.8, 616.0], [53.9, 616.0], [54.0, 618.0], [54.1, 618.0], [54.2, 618.0], [54.3, 618.0], [54.4, 618.0], [54.5, 618.0], [54.6, 618.0], [54.7, 618.0], [54.8, 618.0], [54.9, 618.0], [55.0, 618.0], [55.1, 618.0], [55.2, 618.0], [55.3, 618.0], [55.4, 618.0], [55.5, 618.0], [55.6, 618.0], [55.7, 618.0], [55.8, 618.0], [55.9, 618.0], [56.0, 619.0], [56.1, 619.0], [56.2, 619.0], [56.3, 619.0], [56.4, 619.0], [56.5, 619.0], [56.6, 619.0], [56.7, 619.0], [56.8, 619.0], [56.9, 619.0], [57.0, 622.0], [57.1, 622.0], [57.2, 622.0], [57.3, 622.0], [57.4, 622.0], [57.5, 622.0], [57.6, 622.0], [57.7, 622.0], [57.8, 622.0], [57.9, 622.0], [58.0, 623.0], [58.1, 623.0], [58.2, 623.0], [58.3, 623.0], [58.4, 623.0], [58.5, 623.0], [58.6, 623.0], [58.7, 623.0], [58.8, 623.0], [58.9, 623.0], [59.0, 628.0], [59.1, 628.0], [59.2, 628.0], [59.3, 628.0], [59.4, 628.0], [59.5, 628.0], [59.6, 628.0], [59.7, 628.0], [59.8, 628.0], [59.9, 628.0], [60.0, 641.0], [60.1, 641.0], [60.2, 641.0], [60.3, 641.0], [60.4, 641.0], [60.5, 641.0], [60.6, 641.0], [60.7, 641.0], [60.8, 641.0], [60.9, 641.0], [61.0, 643.0], [61.1, 643.0], [61.2, 643.0], [61.3, 643.0], [61.4, 643.0], [61.5, 643.0], [61.6, 643.0], [61.7, 643.0], [61.8, 643.0], [61.9, 643.0], [62.0, 646.0], [62.1, 646.0], [62.2, 646.0], [62.3, 646.0], [62.4, 646.0], [62.5, 646.0], [62.6, 646.0], [62.7, 646.0], [62.8, 646.0], [62.9, 646.0], [63.0, 648.0], [63.1, 648.0], [63.2, 648.0], [63.3, 648.0], [63.4, 648.0], [63.5, 648.0], [63.6, 648.0], [63.7, 648.0], [63.8, 648.0], [63.9, 648.0], [64.0, 661.0], [64.1, 661.0], [64.2, 661.0], [64.3, 661.0], [64.4, 661.0], [64.5, 661.0], [64.6, 661.0], [64.7, 661.0], [64.8, 661.0], [64.9, 661.0], [65.0, 662.0], [65.1, 662.0], [65.2, 662.0], [65.3, 662.0], [65.4, 662.0], [65.5, 662.0], [65.6, 662.0], [65.7, 662.0], [65.8, 662.0], [65.9, 662.0], [66.0, 690.0], [66.1, 690.0], [66.2, 690.0], [66.3, 690.0], [66.4, 690.0], [66.5, 690.0], [66.6, 690.0], [66.7, 690.0], [66.8, 690.0], [66.9, 690.0], [67.0, 691.0], [67.1, 691.0], [67.2, 691.0], [67.3, 691.0], [67.4, 691.0], [67.5, 691.0], [67.6, 691.0], [67.7, 691.0], [67.8, 691.0], [67.9, 691.0], [68.0, 715.0], [68.1, 715.0], [68.2, 715.0], [68.3, 715.0], [68.4, 715.0], [68.5, 715.0], [68.6, 715.0], [68.7, 715.0], [68.8, 715.0], [68.9, 715.0], [69.0, 732.0], [69.1, 732.0], [69.2, 732.0], [69.3, 732.0], [69.4, 732.0], [69.5, 732.0], [69.6, 732.0], [69.7, 732.0], [69.8, 732.0], [69.9, 732.0], [70.0, 749.0], [70.1, 749.0], [70.2, 749.0], [70.3, 749.0], [70.4, 749.0], [70.5, 749.0], [70.6, 749.0], [70.7, 749.0], [70.8, 749.0], [70.9, 749.0], [71.0, 765.0], [71.1, 765.0], [71.2, 765.0], [71.3, 765.0], [71.4, 765.0], [71.5, 765.0], [71.6, 765.0], [71.7, 765.0], [71.8, 765.0], [71.9, 765.0], [72.0, 795.0], [72.1, 795.0], [72.2, 795.0], [72.3, 795.0], [72.4, 795.0], [72.5, 795.0], [72.6, 795.0], [72.7, 795.0], [72.8, 795.0], [72.9, 795.0], [73.0, 795.0], [73.1, 795.0], [73.2, 795.0], [73.3, 795.0], [73.4, 795.0], [73.5, 795.0], [73.6, 795.0], [73.7, 795.0], [73.8, 795.0], [73.9, 795.0], [74.0, 796.0], [74.1, 796.0], [74.2, 796.0], [74.3, 796.0], [74.4, 796.0], [74.5, 796.0], [74.6, 796.0], [74.7, 796.0], [74.8, 796.0], [74.9, 796.0], [75.0, 797.0], [75.1, 797.0], [75.2, 797.0], [75.3, 797.0], [75.4, 797.0], [75.5, 797.0], [75.6, 797.0], [75.7, 797.0], [75.8, 797.0], [75.9, 797.0], [76.0, 819.0], [76.1, 819.0], [76.2, 819.0], [76.3, 819.0], [76.4, 819.0], [76.5, 819.0], [76.6, 819.0], [76.7, 819.0], [76.8, 819.0], [76.9, 819.0], [77.0, 823.0], [77.1, 823.0], [77.2, 823.0], [77.3, 823.0], [77.4, 823.0], [77.5, 823.0], [77.6, 823.0], [77.7, 823.0], [77.8, 823.0], [77.9, 823.0], [78.0, 826.0], [78.1, 826.0], [78.2, 826.0], [78.3, 826.0], [78.4, 826.0], [78.5, 826.0], [78.6, 826.0], [78.7, 826.0], [78.8, 826.0], [78.9, 826.0], [79.0, 829.0], [79.1, 829.0], [79.2, 829.0], [79.3, 829.0], [79.4, 829.0], [79.5, 829.0], [79.6, 829.0], [79.7, 829.0], [79.8, 829.0], [79.9, 829.0], [80.0, 832.0], [80.1, 832.0], [80.2, 832.0], [80.3, 832.0], [80.4, 832.0], [80.5, 832.0], [80.6, 832.0], [80.7, 832.0], [80.8, 832.0], [80.9, 832.0], [81.0, 846.0], [81.1, 846.0], [81.2, 846.0], [81.3, 846.0], [81.4, 846.0], [81.5, 846.0], [81.6, 846.0], [81.7, 846.0], [81.8, 846.0], [81.9, 846.0], [82.0, 853.0], [82.1, 853.0], [82.2, 853.0], [82.3, 853.0], [82.4, 853.0], [82.5, 853.0], [82.6, 853.0], [82.7, 853.0], [82.8, 853.0], [82.9, 853.0], [83.0, 855.0], [83.1, 855.0], [83.2, 855.0], [83.3, 855.0], [83.4, 855.0], [83.5, 855.0], [83.6, 855.0], [83.7, 855.0], [83.8, 855.0], [83.9, 855.0], [84.0, 859.0], [84.1, 859.0], [84.2, 859.0], [84.3, 859.0], [84.4, 859.0], [84.5, 859.0], [84.6, 859.0], [84.7, 859.0], [84.8, 859.0], [84.9, 859.0], [85.0, 906.0], [85.1, 906.0], [85.2, 906.0], [85.3, 906.0], [85.4, 906.0], [85.5, 906.0], [85.6, 906.0], [85.7, 906.0], [85.8, 906.0], [85.9, 906.0], [86.0, 923.0], [86.1, 923.0], [86.2, 923.0], [86.3, 923.0], [86.4, 923.0], [86.5, 923.0], [86.6, 923.0], [86.7, 923.0], [86.8, 923.0], [86.9, 923.0], [87.0, 932.0], [87.1, 932.0], [87.2, 932.0], [87.3, 932.0], [87.4, 932.0], [87.5, 932.0], [87.6, 932.0], [87.7, 932.0], [87.8, 932.0], [87.9, 932.0], [88.0, 933.0], [88.1, 933.0], [88.2, 933.0], [88.3, 933.0], [88.4, 933.0], [88.5, 933.0], [88.6, 933.0], [88.7, 933.0], [88.8, 933.0], [88.9, 933.0], [89.0, 962.0], [89.1, 962.0], [89.2, 962.0], [89.3, 962.0], [89.4, 962.0], [89.5, 962.0], [89.6, 962.0], [89.7, 962.0], [89.8, 962.0], [89.9, 962.0], [90.0, 963.0], [90.1, 963.0], [90.2, 963.0], [90.3, 963.0], [90.4, 963.0], [90.5, 963.0], [90.6, 963.0], [90.7, 963.0], [90.8, 963.0], [90.9, 963.0], [91.0, 976.0], [91.1, 976.0], [91.2, 976.0], [91.3, 976.0], [91.4, 976.0], [91.5, 976.0], [91.6, 976.0], [91.7, 976.0], [91.8, 976.0], [91.9, 976.0], [92.0, 1106.0], [92.1, 1106.0], [92.2, 1106.0], [92.3, 1106.0], [92.4, 1106.0], [92.5, 1106.0], [92.6, 1106.0], [92.7, 1106.0], [92.8, 1106.0], [92.9, 1106.0], [93.0, 1121.0], [93.1, 1121.0], [93.2, 1121.0], [93.3, 1121.0], [93.4, 1121.0], [93.5, 1121.0], [93.6, 1121.0], [93.7, 1121.0], [93.8, 1121.0], [93.9, 1121.0], [94.0, 1234.0], [94.1, 1234.0], [94.2, 1234.0], [94.3, 1234.0], [94.4, 1234.0], [94.5, 1234.0], [94.6, 1234.0], [94.7, 1234.0], [94.8, 1234.0], [94.9, 1234.0], [95.0, 1321.0], [95.1, 1321.0], [95.2, 1321.0], [95.3, 1321.0], [95.4, 1321.0], [95.5, 1321.0], [95.6, 1321.0], [95.7, 1321.0], [95.8, 1321.0], [95.9, 1321.0], [96.0, 1385.0], [96.1, 1385.0], [96.2, 1385.0], [96.3, 1385.0], [96.4, 1385.0], [96.5, 1385.0], [96.6, 1385.0], [96.7, 1385.0], [96.8, 1385.0], [96.9, 1385.0], [97.0, 1636.0], [97.1, 1636.0], [97.2, 1636.0], [97.3, 1636.0], [97.4, 1636.0], [97.5, 1636.0], [97.6, 1636.0], [97.7, 1636.0], [97.8, 1636.0], [97.9, 1636.0], [98.0, 1741.0], [98.1, 1741.0], [98.2, 1741.0], [98.3, 1741.0], [98.4, 1741.0], [98.5, 1741.0], [98.6, 1741.0], [98.7, 1741.0], [98.8, 1741.0], [98.9, 1741.0], [99.0, 1778.0], [99.1, 1778.0], [99.2, 1778.0], [99.3, 1778.0], [99.4, 1778.0], [99.5, 1778.0], [99.6, 1778.0], [99.7, 1778.0], [99.8, 1778.0], [99.9, 1778.0]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 100.0, "title": "Response Time Percentiles"}},
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
        data: {"result": {"minY": 1.0, "minX": 500.0, "maxY": 45.0, "series": [{"data": [[1100.0, 2.0], [1200.0, 1.0], [600.0, 23.0], [1300.0, 2.0], [700.0, 8.0], [1600.0, 1.0], [800.0, 9.0], [1700.0, 2.0], [900.0, 7.0], [500.0, 45.0]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 100, "maxX": 1700.0, "title": "Response Time Distribution"}},
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
        data: {"result": {"minY": 3.0, "minX": 1.0, "ticks": [[0, "Requests having \nresponse time <= 500ms"], [1, "Requests having \nresponse time > 500ms and <= 1,500ms"], [2, "Requests having \nresponse time > 1,500ms"], [3, "Requests in error"]], "maxY": 97.0, "series": [{"data": [], "color": "#9ACD32", "isOverall": false, "label": "Requests having \nresponse time <= 500ms", "isController": false}, {"data": [[1.0, 97.0]], "color": "yellow", "isOverall": false, "label": "Requests having \nresponse time > 500ms and <= 1,500ms", "isController": false}, {"data": [[2.0, 3.0]], "color": "orange", "isOverall": false, "label": "Requests having \nresponse time > 1,500ms", "isController": false}, {"data": [], "color": "#FF6347", "isOverall": false, "label": "Requests in error", "isController": false}], "supportsControllersDiscrimination": false, "maxX": 2.0, "title": "Synthetic Response Times Distribution"}},
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
        data: {"result": {"minY": 5.149999999999999, "minX": 1.7918259E12, "maxY": 5.149999999999999, "series": [{"data": [[1.7918259E12, 5.149999999999999]], "isOverall": false, "label": "Thread Group", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918259E12, "title": "Active Threads Over Time"}},
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
        data: {"result": {"minY": 560.046511627907, "minX": 1.0, "maxY": 1741.0, "series": [{"data": [[8.0, 963.0], [2.0, 819.0], [9.0, 1707.0], [10.0, 1741.0], [11.0, 1121.0], [12.0, 1321.0], [3.0, 855.0], [13.0, 1309.5], [4.0, 560.046511627907], [1.0, 853.0], [5.0, 654.5000000000001], [6.0, 790.3571428571428], [7.0, 977.75]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}, {"data": [[5.149999999999999, 710.8999999999999]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler-Aggregated", "isController": false}], "supportsControllersDiscrimination": true, "maxX": 13.0, "title": "Time VS Threads"}},
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
        data : {"result": {"minY": 1285.0, "minX": 1.7918259E12, "maxY": 20751.666666666668, "series": [{"data": [[1.7918259E12, 20751.666666666668]], "isOverall": false, "label": "Bytes received per second", "isController": false}, {"data": [[1.7918259E12, 1285.0]], "isOverall": false, "label": "Bytes sent per second", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918259E12, "title": "Bytes Throughput Over Time"}},
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
        data: {"result": {"minY": 710.8999999999999, "minX": 1.7918259E12, "maxY": 710.8999999999999, "series": [{"data": [[1.7918259E12, 710.8999999999999]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918259E12, "title": "Response Time Over Time"}},
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
        data: {"result": {"minY": 710.43, "minX": 1.7918259E12, "maxY": 710.43, "series": [{"data": [[1.7918259E12, 710.43]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918259E12, "title": "Latencies Over Time"}},
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
        data: {"result": {"minY": 4.620000000000001, "minX": 1.7918259E12, "maxY": 4.620000000000001, "series": [{"data": [[1.7918259E12, 4.620000000000001]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918259E12, "title": "Connect Time Over Time"}},
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
        data: {"result": {"minY": 501.0, "minX": 1.7918259E12, "maxY": 1778.0, "series": [{"data": [[1.7918259E12, 1778.0]], "isOverall": false, "label": "Max", "isController": false}, {"data": [[1.7918259E12, 501.0]], "isOverall": false, "label": "Min", "isController": false}, {"data": [[1.7918259E12, 962.9]], "isOverall": false, "label": "90th percentile", "isController": false}, {"data": [[1.7918259E12, 1777.6299999999999]], "isOverall": false, "label": "99th percentile", "isController": false}, {"data": [[1.7918259E12, 611.0]], "isOverall": false, "label": "Median", "isController": false}, {"data": [[1.7918259E12, 1316.649999999999]], "isOverall": false, "label": "95th percentile", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918259E12, "title": "Response Time Percentiles Over Time (successful requests only)"}},
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
    data: {"result": {"minY": 565.0, "minX": 1.0, "maxY": 1106.0, "series": [{"data": [[1.0, 1087.0], [8.0, 720.0], [9.0, 565.0], [5.0, 792.0], [6.0, 591.0], [13.0, 1106.0], [7.0, 587.5]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 13.0, "title": "Response Time Vs Request"}},
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
    data: {"result": {"minY": 564.0, "minX": 1.0, "maxY": 1105.0, "series": [{"data": [[1.0, 1078.5], [8.0, 719.5], [9.0, 564.0], [5.0, 792.0], [6.0, 591.0], [13.0, 1105.0], [7.0, 587.5]], "isOverall": false, "label": "Successes", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 1000, "maxX": 13.0, "title": "Latencies Vs Request"}},
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
        data: {"result": {"minY": 0.08333333333333333, "minX": 1.79182584E12, "maxY": 1.5833333333333333, "series": [{"data": [[1.79182584E12, 0.08333333333333333], [1.7918259E12, 1.5833333333333333]], "isOverall": false, "label": "hitsPerSecond", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918259E12, "title": "Hits Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.7918259E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.7918259E12, 1.6666666666666667]], "isOverall": false, "label": "200", "isController": false}], "supportsControllersDiscrimination": false, "granularity": 60000, "maxX": 1.7918259E12, "title": "Codes Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.7918259E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.7918259E12, 1.6666666666666667]], "isOverall": false, "label": "JM-02-ListNotificationsPeak Sampler-success", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918259E12, "title": "Transactions Per Second"}},
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
        data: {"result": {"minY": 1.6666666666666667, "minX": 1.7918259E12, "maxY": 1.6666666666666667, "series": [{"data": [[1.7918259E12, 1.6666666666666667]], "isOverall": false, "label": "Transaction-success", "isController": false}, {"data": [], "isOverall": false, "label": "Transaction-failure", "isController": false}], "supportsControllersDiscrimination": true, "granularity": 60000, "maxX": 1.7918259E12, "title": "Total Transactions Per Second"}},
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

