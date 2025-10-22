
$hs.html.$popup = {

    new: function (modalId) {

        let _modalId = modalId;
        let _buttonClass = "btn-popup-close";
        let modal = document.querySelector("#" + _modalId);
        let modalContent = document.querySelector(".modal-content");

        let firstOpen = true;

        function openModal() {
            modal.style.display = "block";

            if (firstOpen) {
                centerModal();
                firstOpen = false;
            }
        }

        function closeModal() {
            modal.style.display = "none";
        }

        function centerModal() {
            const modalWidth = modal.offsetWidth;
            const modalHeight = modal.offsetHeight;

            const windowWidth = document.body.offsetWidth;
            const windowHeight = document.body.offsetHeight;

            // 모달의 위치를 중앙으로 설정
            modal.style.left = `${(windowWidth - modalWidth) / 2}px`;
            //modal.style.top = `${(windowHeight - modalHeight) / 2}px`;
            modal.style.top = '100px';
            modal.style.zIndex = '50';
        }

        function addCloseEvents() {
            const closeButtons = modal.querySelectorAll(`.${_buttonClass}`);

            closeButtons.forEach(button => {
                button.addEventListener("click", closeModal);
            });

            //document.addEventListener('keydown', function (e) {
            //    if (e.key === 'Escape') {
            //        closeModal();
            //    }
            //});
        }

        function makeModalDraggable() {

            //const modalHeader = document.querySelector("#" + "modal-header");
            const modalHeader = modal.querySelector(".modal-header");
            modalHeader.style.cursor = "move";
            modalHeader.style.userSelect = "none";

            // 드래그 위한 상태 변수 선언
            let isDragging = false;
            let offsetX = 0;
            let offsetY = 0;

            // mousedown 이벤트 핸들러
            modalHeader.addEventListener('mousedown', function (e) {
                isDragging = true;
                offsetX = e.clientX - modal.offsetLeft;
                offsetY = e.clientY - modal.offsetTop;
            });

            // mousemove 이벤트 핸들러
            modal.addEventListener('mousemove', (e) => {
                if (isDragging) {
                    let newX = e.clientX - offsetX;
                    let newY = e.clientY - offsetY;

                    // 문서 바디의 크기와 모달의 크기를 고려하여 위치 제한
                    const bodyWidth = document.documentElement.offsetWidth;
                    const bodyHeight = document.documentElement.offsetHeight;

                    if (newX < 0) newX = 0;
                    if (newY < 0) newY = 0;
                    if (newX + modalContent.offsetWidth > bodyWidth) newX = bodyWidth - modalContent.offsetWidth;
                    if (newY + modalContent.offsetHeight > bodyHeight) newY = bodyHeight - modalContent.offsetHeight;

                    modal.style.left = `${newX}px`;
                    modal.style.top = `${newY}px`;
                }
            });

            // mouseup 이벤트 핸들러
            modalHeader.addEventListener('mouseup', function () {
                isDragging = false;
            });
        }

        addCloseEvents();
        makeModalDraggable();

        return {
            open: openModal,
            close: closeModal
        };
    },
    init: function (modalId) {
        $hs.controls[modalId] = new $hs.html.$popup.new(modalId);
    }
}

$hs.util.$CommonUtil = {
    checkFavorite: (curMenuId) => {
        var btnFavorite = document.getElementById("favorites-toggle");
        // 즐겨찾기 default false
        var favoriteFlag = false;

        // 현재 즐겨찾기 상태 여부 확인
        $hs.fetch({
            url: "/CmnURL",
            command: "check_favorite",
            param: { terms: { curMenuId: curMenuId } }
        }).then(function (fromServer) {
            btnFavorite.checked = fromServer.data.length > 0;
            favoriteFlag = fromServer.data.length > 0;
        }).catch(function (e) {
            $hs.errorBox(e);
        });

        // 즐겨찾기 버튼 클릭 이벤트 등록
        btnFavorite.addEventListener("click", toggleFavorite);

        function toggleFavorite() {
            // 현재 즐겨찾기 여부에 따라 삭제, 등록으로 다르게 적용
            var command = favoriteFlag ? "delete_favorite" : "add_favorite";

            $hs.fetch({
                url: "/CmnURL",
                command: command,
                param: { terms: { curMenuId: curMenuId } }
            }).then(function () {
                btnFavorite.checked = !favoriteFlag;
                favoriteFlag = !favoriteFlag;
                // TODO : 즐겨찾기 목록에 추가하는거를 해야하는데...
            }).catch(function (e) {
                $hs.errorBox(e);
            });
        }

    },
    /*
        grid 헤더 세팅
        param : grid_id, defaultColumns
        grid_id 세팅시 data-grid-id 정의 필요
    */
    getGridSetting: (grid_id, defaultColumns) => {
        // ex) <div id="resource_info" data-grid-id="RESOURCE"></div>
        // grid로 사용할 id의 data-grid-id 값으로 DB저장 및 조회
        let gridId = document.getElementById(grid_id).dataset.gridId;
        let toServer = {};
        // DB에서 조회할 grid_id 추가
        toServer["terms"] = $hs.$("pnlSearch").val();
        toServer["terms"]["grid_id"] = gridId;


        // 공통 컬럼 추가 함수
        // TODO : dataField랑 label이랑 따로 따로 저장하고 쓰는게 좋을 것 같음..
        const applyColumns = (columns) => {
            $hs.$(grid_id).columns(col => {
                columns.forEach(column => {

                    if (column.columnGroup == null) {
                        col.add({
                            dataField: column.dataField
                            , label: column.label
                            , width: column.width
                            , visible: (column.visible === undefined || column.visible === true || column.visible === "True" || column.visible == '1')
                            , fixed: column.fixed === true || column.fixed === "True" || column.fixed == '1' ? true : false
                            , editable: column.editable === true || column.editable === "True" || column.editable == '1' ? true : false
                            , displayField: column.displayField ? column.displayField : null
                            , popValueField: column.popValueField ? column.popValueField : null
                            , popTextField: column.popTextField ? column.popTextField : null
                            , type: column.type ? column.type : null
                            , grid: column.grid ? column.grid : null
                            , align: column.align ? column.align : null
                            , precision: column.precision ? column.precision : null
                            , buttonLabel: column.buttonLabel ? column.buttonLabel : null
                            , useTemplate: column.useTemplate ? column.useTemplate : null
                            , ratio: column.ratio ? column.ratio : null
                            , format: column.format ? column.format : null
                        });
                    } else {
                        col.add({
                            dataField: column.dataField
                            , label: column.label
                            , width: column.width
                            , visible: (column.visible === undefined || column.visible === true || column.visible === "True" || column.visible == '1')
                            , fixed: column.fixed === true || column.fixed === "True" || column.fixed == '1' ? true : false
                            , editable: column.editable === true || column.editable === "True" || column.editable == '1' ? true : false
                            , displayField: column.displayField ? column.displayField : null
                            , popValueField: column.popValueField ? column.popValueField : null
                            , popTextField: column.popTextField ? column.popTextField : null
                            , type: column.type ? column.type : null
                            , grid: column.grid ? column.grid : null
                            , align: column.align ? column.align : null
                            , precision: column.precision ? column.precision : null
                            , buttonLabel: column.buttonLabel ? column.buttonLabel : null
                            , useTemplate: column.useTemplate ? column.useTemplate : null
                            , ratio: column.ratio ? column.ratio : null
                            , format: column.format ? column.format : null
                            , columnGroup: column.columnGroup ? column.columnGroup : null
                        });
                    }

                    if (column.displayField != null) {
                        //console.log(column.grid);
                        //console.log(col);
                    }
                });
            });
        };

        const checkEtcFunction = (serverColumns, defaultColumns) => {
            if (serverColumns && serverColumns.length > 0) {
                return serverColumns.map(s_col => {
                    // 기본적으로 서버 컬럼 복사
                    let mergedCol = { ...s_col };

                    // 같은 dataField를 가진 default 컬럼 찾기
                    const defaultCol = defaultColumns.find(d_col => d_col.dataField === s_col.dataField);

                    if (defaultCol) {
                        // defaultCol에 있는 모든 key 중에서 mergedCol에 없는 key만 복사
                        Object.keys(defaultCol).forEach(key => {
                            if (mergedCol[key] === undefined) {
                                mergedCol[key] = defaultCol[key];
                            }
                        });
                    }

                    return mergedCol;
                });
            } else {
                // 서버에서 아무것도 안 내려오면 기본 컬럼 그대로 사용
                return defaultColumns;
            }
        };
        //console.log(toServer);
        // 서버 호출 후 컬럼 처리
        $hs.fetch({
            url: "/CmnURL",
            command: "search_grid",
            param: toServer
        }).then(fromServer => {
            const data = fromServer["data"];
            // defaultColumns에 displayField 등 DB에 저장되지 않는 다른 값들을 넣는 로직
            // checkEtcFunction(data, defaultColumns); // data, defaultColumns 두개를 줘서 비교한다음에 etc 데이터가 있으면 붙여주는 역할 필요.
            const view_data = checkEtcFunction(data, defaultColumns);
            applyColumns(view_data);
            //console.log(data);
            //console.log(toServer);

            if (typeof onDone === "function") {
                onDone();
            }
        }).catch(e => {
            console.log(e); //$hs.errorBox(e);  
        });
    },
    /*
        grid 헤더 세팅 modal open
        param : modal_id, grid_id, dimension_grid_id
    */
    openGridSettingModal: (modal_id, grid_id, dimension_grid_id) => {
        $hs.$(modal_id).open();

        // 화면에서 변경된 헤더 정보를 가져오기 위한 로직
        var gridInstance = $hs.$(grid_id)._instance; // devExtreme 순수 instance 가져옴

        var currentColumns = [];
        var columnCount = gridInstance.columnCount();

        for (var i = 0; i < columnCount; i++) {
            var col = gridInstance.columnOption(i);
            
            if (col.dataField != "_key" && col.dataField != null)  // _key 값인 ROW는 추가하지 않는다.
            {
                //console.log(col.dataField == null);
                currentColumns.push({
                    dataField: col.dataField, // 컬럼 키
                    editable: col.editable,
                    name: col.label,    // 화면 표시용 이름
                    visible: col.visible,   // 보이는 여부
                    width: col.width,       // 너비
                    fix: col.fixed          // 고정 여부 (없으면 false)
                });
            }
        }
        // 로직 end

        $hs.$(dimension_grid_id).data(currentColumns);
    },
    /*
        grid 헤더 세팅 DB Save
        param : modal_id, grid_id, dimension_grid_id
    */
    saveGridSetting: (modal_id, grid_id, dimension_grid_id, defaultColumns) => {
        // 저장할 데이터 담는 변수
        let toServer = {};

        // 그리드의 dataset.gridId를 불러온다
        let gridId = document.getElementById(grid_id).dataset.gridId;

        // dimension 그리드에서 변경한 값들을 가져온다.
        var dimension_grid_info = JSON.stringify($hs.$(dimension_grid_id).data());

        var dimension_data_list = [];
        // 변경된 값 가져와서 DB 테이블에 넣기 위해 재정의
        JSON.parse(dimension_grid_info).forEach((data, idx) => {
            let dimension_data = {
                grid_id: gridId,
                dataField: data.dataField,
                label: data.name,
                visible: data.visible,
                width: data.width,
                fixed: data.fix,
                editable: data.editable,
                column_order: idx  // 여기서 idx를 써서 순서 표시
            };

            let extra = defaultColumns.find(def => def.dataField === data.dataField) || {};

            let merged_data = {
                ...extra,
                ...dimension_data
            };

            dimension_data_list.push(merged_data);
        });

        // DB에 저장 후 화면에 적용
        toServer["data"] = dimension_data_list;

        $hs.fetch({
            url: "/CmnURL",
            command: "save_grid",
            param: toServer
        }).then(fromServer => {
            alert("저장 되었습니다.");

            // 동적 헤더 컬럼 추가
            $hs.$(grid_id).columns(col => {
                dimension_data_list.forEach(column => col.add(column));
            });

            $hs.$(modal_id).close();
        }).catch(e => $hs.errorBox(e))
    },
    /*
        grid 헤더 우클릭 세팅
        param :  data
    */
    getHeaderContextClick: (data) => {

       const gridInstance = data.$._instance;
       const field = data.dataField;

       // 컬럼 정렬 'asc'
       if (data.menu == "Sort Asc") {
           gridInstance.columnOption(field, 'sortOrder', 'asc');
       }
       // 컬럼 정렬 'asc'
       if (data.menu == "Sort Desc") {
           gridInstance.columnOption(field, 'sortOrder', 'desc');
       }
       // 컬럼 정렬 취소
       if (data.menu == "Sort Clear") {
           gridInstance.columnOption(field, 'sortOrder', null);
       }
       // 컬럼 고정
       if (data.menu == "Fixed") {
           gridInstance.columnOption(field, {
               fixed: true,
               fixedPosition: 'left' // 또는 'right'
           });
       }
       // 컬럼 고정 취소
       if (data.menu == "Unfixed") {
           gridInstance.columnOption(field, {
               fixed: false,
               fixedPosition: undefined
           });
       }

       // 컬럼 숨기기
       if (data.menu == "Hide") {
           const columns = gridInstance.getVisibleColumns();
           const columnIndex = columns.findIndex(col => col.dataField === field);

           if (columnIndex !== -1) {
               gridInstance.columnOption(field, 'visible', false);
           }
       }
       // 컬럼 숨기기 취소
       if (data.menu == "Hide Clear") {
           const columns = gridInstance.option("columns");

           // 전체 컬럼을 순회하면서 숨겨진 컬럼을 찾아서 보이게 하기
           columns.forEach(col => {
               //console.log(col.dataField);
               if (col.dataField != "_key") {
                   gridInstance.columnOption(col.dataField, {
                       visible: true,  // 해당 컬럼을 보이게 함
                       groupIndex: undefined,
                   });
               }
           });
        }

        //----------------------------------- GRID 계산식 -------------------------------------------------//

        const items = gridInstance.getDataSource().items();
        const values = items.map(row => row[field]).filter(v => v != null && v !== "");

        const isAllNumeric = values.every(v => typeof v === "number" && !isNaN(v));

        const grid_settings = ["Sort Asc", "Sort Desc", "Sort Clear", "Fixed", "Unfixed", "Hide", "Hide Clear"];
        const isCAL = grid_settings.includes(data.menu); // true면 grid setting용 false면 계산기용

        //if (!isCAL) {
        //    alert("해당 계산은 숫자형 필드에만 사용할 수 있습니다.");
        //    return;
        //}

        let result = null;
        let cal_class = null; // 중복방지 체크용

        if (data.menu == "CAL-Count") {
            cal_class = 'count';
            result = '총 ' + values.length + '개';
        }

        if (data.menu == "CAL-Avg") {
            cal_class = 'average';
            result = '평균 :' + (values.reduce((a, b) => a + b, 0) / values.length).toFixed(2);
        }

        if (data.menu == "CAL-Max") {
            cal_class = 'max';
            result = '최댓값 : ' + Math.max(...values);
        }

        if (data.menu == "CAL-Min") {
            cal_class = 'min';
            result = '최솟값 : ' + Math.min(...values);
        }

        if (data.menu == "CAL-Sum") {
            cal_class = 'sum';
            result = '합계 : ' + values.reduce((a, b) => a + b, 0);
        }

        if (data.menu == "CAL-Std. Div") {
            cal_class = 'std_div';
            const avg = values.reduce((a, b) => a + b, 0) / values.length;
            result = '표준편차 : ' + Math.sqrt(values.reduce((a, b) => a + Math.pow(b - avg, 2), 0) / values.length).toFixed(2);
        }
       /// console.log('result');
       // console.log(result);

        // 1. class 확인
        const class_name = $('#grid_cal').attr('class');
        const is_null_class_name = class_name === '';
        if (is_null_class_name) {
            // class_name이 없다면 우클릭한 컬럼 이름으로 클래스 지정
            $('#grid_cal').attr('class', field);
            $('#grid_cal').text(field + ' ' + result);
        } else {
            // class_name이 있다면 기존 클래스 이름이랑 같은지 먼저 비교
            if ($('#grid_cal').attr('class') === field) {
                // 같다면 결과값만 추가.
                // 같은 수식을 클릭한다면..? 해결해야할 사항....
                $('#grid_cal').append(', ' + result);
            } else {
                // 다르다면
                // 기존 클래스 제거 후 우클릭한 컬럼 이름으로 클래스 지정
                $('#grid_cal').attr('class', '');
                $('#grid_cal').attr('class', field);
                $('#grid_cal').text(field + ' ' + result);
            }    
        }
        
    },
    /*
        엑셀 to GRID 복사 붙여넣기
        param :  grid_id
    */
    setExcelToGridPaste: (grid_id) => {
        const gridInstance = $hs.$(grid_id)._instance;

        if (gridInstance) {
            const gridElement = document.querySelector('#' + grid_id);

            gridElement.addEventListener('paste', function (event) {
                const clipboard = event.clipboardData || window.clipboardData;
                const clipboardText = clipboard.getData('Text');
                handlePaste(gridInstance, clipboardText);
            });
        }

        function handlePaste(gridInstance, text) {
            const rows = text.trim().split(/\r\n|\n|\r/).map(row => row.split('\t'));
            if (!rows || rows.length === 0) return;

            const visibleColumns = gridInstance.getVisibleColumns();
            const currentData = gridInstance.option("dataSource") || [];
            const focusedRowIndex = gridInstance.option("focusedRowIndex") ?? 0;
            const focusedColumnIndex = gridInstance.option("focusedColumnIndex") ?? 0;

            // ✨ editable 컬럼만 필터링 (index 정보 포함)
            const editableColumns = visibleColumns
                .map((col, index) => ({ ...col, _index: index }))
                .filter(col => col.allowEditing !== false && col.dataField);

            // 🎯 focusedColumn 기준으로 editable 시작 인덱스 계산 (visibleColumns 기준)
            const focusedColumn = visibleColumns[focusedColumnIndex];
            const editableVisibleIndexes = editableColumns.map(c => c._index);
            const startEditableIndex = editableVisibleIndexes.findIndex(idx => idx >= focusedColumnIndex);

            if (startEditableIndex === -1) {
                console.warn("복사 시작 컬럼이 편집 가능한 컬럼이 아님");
                return;
            }

            // 필요한 행 수 계산
            const requiredRowCount = focusedRowIndex + rows.length;
            const currentRowCount = currentData.length;
            const rowsToAdd = requiredRowCount - currentRowCount;

            // 고유 키 설정
            const keyField = gridInstance.option("keyExpr") || "_key";
            let maxId = Math.max(0, ...currentData.map(d => d[keyField] || 0));

            const newData = [...currentData]; // ⚠️ 새 배열로 복사해서 적용

            // 필요한 만큼 행 추가
            for (let i = 0; i < rowsToAdd; i++) {
                const row = {};
                row[keyField] = ++maxId;
                newData.push(row);
            }

            // 붙여넣기 처리
            rows.forEach((rowData, rowOffset) => {
                const rowIndex = focusedRowIndex + rowOffset;

                rowData.forEach((cellData, dataOffset) => {
                    const targetCol = editableColumns[startEditableIndex + dataOffset];
                    if (targetCol) {
                        let value = cellData;

                        // 숫자형 컬럼이면 쉼표 제거 후 숫자로 변환
                        if (targetCol.dataType === "number") {
                            value = parseFloat(cellData.replace(/,/g, ''));
                        }

                        newData[rowIndex][targetCol.dataField] = value;
                        //newData[rowIndex][targetCol.dataField] = cellData;
                    }
                });
            });

            // 🔄 새로운 배열로 dataSource 갱신
            gridInstance.option("dataSource", newData);
        }
    },
    /*
        grid 안에 데이터로 엑셀 다운로드하기
    */
    excelDownload: (grid_id, fileName) => {
        const gridInstance = $("#" + grid_id).dxDataGrid("instance");
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet("Export");


        // 컬럼 수 가져오기
        const columnCount = gridInstance.getVisibleColumns().length;

        // 전체 컬럼 너비 20으로 설정
        worksheet.columns = Array.from({ length: columnCount }, () => ({ width: 20 }));
        

        // 현재일시(yyyyMMddHHmmss) 구하기
        var date = new Date();
        var year = date.getFullYear().toString();

        var month = date.getMonth() + 1;
        month = month < 10 ? '0' + month.toString() : month.toString();

        var day = date.getDate();
        day = day < 10 ? '0' + day.toString() : day.toString();

        var hour = date.getHours();
        var min = date.getMinutes();
        var sec = date.getSeconds();

        var now = year + month + day + hour + min + sec;
        // -------------------------------------------------

        const download_file_name = fileName + "_" + now + ".xlsx";

        DevExpress.excelExporter.exportDataGrid({
            component: gridInstance,
            worksheet: worksheet,
            autoFilterEnabled: true,
            customizeCell: ({ gridCell, excelCell }) => {
                const column = gridCell.column;
                const rowType = gridCell.rowType;

                        // 데이터 셀만 처리
                if (gridCell.rowType === "data") {
                    const value = gridCell.value;
                              // 숫자일 경우 천 단위 구분 서식 적용
                    if (typeof value === "number") {
                        excelCell.value = value;
                        excelCell.numFmt = '#,##0'; // 천 단위 쉼표 표시
                    } else if (
                        value === "" ||
                        value === " " ||
                        (typeof value === "string" && value.trim() === "") ||
                        value === undefined
                    ) {
                        excelCell.value = null;
                    }
                }
            }
        }).then(() => {
            // 컬럼 너비 다시 고정
            //const visibleColumns = gridInstance.getVisibleColumns();

            //visibleColumns.forEach((col, index) => {
            //    worksheet.getColumn(index + 1).width = 20; // 각 열 너비 고정
            //});


            const columnCount = worksheet.actualColumnCount || gridInstance.getVisibleColumns().length;
           // console.log(columnCount);
            for (let i = 1; i <= columnCount; i++) {
                worksheet.getColumn(i).width = 20;
            }


            return workbook.xlsx.writeBuffer();
        }).then((buffer) => {
            saveAs(new Blob([buffer], { type: "application/octet-stream" }), download_file_name);
        });
    },

    /*
        grid 안에 데이터로 엑셀 다운로드하기
    */
    excelDownloadFordeliviery_capaload: (grid_id, fileName) => {
        const gridInstance = $("#" + grid_id).dxDataGrid("instance");
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet("Export");


        // 컬럼 수 가져오기
        const columnCount = gridInstance.getVisibleColumns().length;

        // 전체 컬럼 너비 20으로 설정
        worksheet.columns = Array.from({ length: columnCount }, () => ({ width: 20 }));


        // 현재일시(yyyyMMddHHmmss) 구하기
        var date = new Date();
        var year = date.getFullYear().toString();

        var month = date.getMonth() + 1;
        month = month < 10 ? '0' + month.toString() : month.toString();

        var day = date.getDate();
        day = day < 10 ? '0' + day.toString() : day.toString();

        var hour = date.getHours();
        var min = date.getMinutes();
        var sec = date.getSeconds();

        var now = year + month + day + hour + min + sec;
        // -------------------------------------------------

        const download_file_name = fileName + "_" + now + ".xlsx";

        DevExpress.excelExporter.exportDataGrid({
            component: gridInstance,
            worksheet: worksheet,
            autoFilterEnabled: true,
            customizeCell: ({ gridCell, excelCell }) => {
                const column = gridCell.column;
                const rowType = gridCell.rowType;
                const rowData = gridCell.data; // ✅ rowData 접근

                // 컬럼 이름이 날짜 형식이면 → 주말 여부 판단
                const columnKey = column.dataField || column.caption;
                const date = new Date(columnKey);

                // ✅ 테두리도 같이 지정
                excelCell.border = {
                    top: { style: "thin" },
                    left: { style: "thin" },
                    bottom: { style: "thin" },
                    right: { style: "thin" }
                };

                const isWeekend = !isNaN(date.getTime()) && (date.getDay() === 0 || date.getDay() === 6);

                if (isWeekend) {
                    // 헤더든 데이터든 주말 컬럼이면 배경색 지정
                    excelCell.fill = {
                        type: "pattern",
                        pattern: "solid",
                        fgColor: { argb: "F0FFF0" } // 연노랑
                    };
                }

                // ✅ 누적 Balance 행 배경 처리
                if (rowType === "data" && rowData?.CATEGORY_NAME === "누적 Balance") {
                    excelCell.fill = {
                        type: "pattern",
                        pattern: "solid",
                        fgColor: { argb: "E2EFDA" } // 연녹색
                    };
                }



                // 데이터 셀만 처리
                if (gridCell.rowType === "data") {
                    const value = gridCell.value;
                    // 숫자일 경우 천 단위 구분 서식 적용
                    if (typeof value === "number") {
                        excelCell.value = value;
                        excelCell.numFmt = '#,##0'; // 천 단위 쉼표 표시

                        // 음수일 경우 빨간 글자
                        if (value < 0) {
                            excelCell.font = {
                                color: { argb: 'FF0000' } // 빨간색
                            };
                        }

                    } else if (
                        value === "" ||
                        value === " " ||
                        (typeof value === "string" && value.trim() === "") ||
                        value === undefined
                    ) {
                        excelCell.value = null;
                    }
                }
            }
        }).then(() => {
            // 컬럼 너비 다시 고정
            //const visibleColumns = gridInstance.getVisibleColumns();

            //visibleColumns.forEach((col, index) => {
            //    worksheet.getColumn(index + 1).width = 20; // 각 열 너비 고정
            //});


            const columnCount = worksheet.actualColumnCount || gridInstance.getVisibleColumns().length;
            // console.log(columnCount);
            for (let i = 1; i <= columnCount; i++) {
                worksheet.getColumn(i).width = 20;
            }


            return workbook.xlsx.writeBuffer();
        }).then((buffer) => {
            saveAs(new Blob([buffer], { type: "application/octet-stream" }), download_file_name);
        });
    },

    excelDownloadVerticalMerged: (grids, fileName) => {
        const workbook = new ExcelJS.Workbook();

        // 첫 번째 그리드는 단일 시트
        const sheet1 = workbook.addWorksheet("Routing");

        DevExpress.excelExporter.exportDataGrid({
            component: $("#" + grids[0].gridId).dxDataGrid("instance"),
            worksheet: sheet1,
            autoFilterEnabled: true
        }).then(() => {
            // 나머지 그리드들을 묶어서 하나의 시트에 세로로 병합
            const sheet2 = workbook.addWorksheet("Spec BOM");
            let currentRow = 0;

            const exportNext = (index) => {
                if (index >= grids.length) {
                    // 모든 export가 끝난 후 저장
                    workbook.xlsx.writeBuffer().then((buffer) => {
                        const blob = new Blob([buffer], { type: "application/octet-stream" });
                        saveAs(blob, `${fileName}.xlsx`);
                    });
                    return;
                }

                const gridInfo = grids[index];
                const gridInstance = $("#" + gridInfo.gridId).dxDataGrid("instance");

                // 시트에 제목 넣기 (선택)
                //const title = `<< ${gridInfo.title || "그리드 " + index} >>`;
                //sheet2.getCell(`A${currentRow}`).value = title;
                sheet2.getCell(`A${currentRow}`).font = { bold: true };
                currentRow += 1;

                DevExpress.excelExporter.exportDataGrid({
                    component: gridInstance,
                    worksheet: sheet2,
                    topLeftCell: { row: currentRow, column: 1 },
                    autoFilterEnabled: true
                }).then(() => {
                    const rowCount = gridInstance.getVisibleRows().length;
                    currentRow += rowCount + 4; // 헤더 + 여백
                    exportNext(index + 1); // 다음 그리드 처리
                });
            };

            // 두 번째 그리드부터 처리 (index 1부터)
            exportNext(1);
        });
    },

    excelDownloadPerGridSheet: (grids, fileName) => {
        const workbook = new ExcelJS.Workbook();

        const exportNext = (index) => {
            if (index >= grids.length) {
                // 모든 export가 끝난 후 저장
                workbook.xlsx.writeBuffer().then((buffer) => {
                    const blob = new Blob([buffer], { type: "application/octet-stream" });
                    saveAs(blob, `${fileName}.xlsx`);
                });
                return;
            }

            const gridInfo = grids[index];
            const gridInstance = $("#" + gridInfo.gridId).dxDataGrid("instance");

            // 시트 이름 설정 (중복 방지)
            const sheetName = gridInfo.title || `Sheet${index + 1}`;
            const sheet = workbook.addWorksheet(sheetName.substring(0, 31)); // Excel 시트 이름은 최대 31자

            DevExpress.excelExporter.exportDataGrid({
                component: gridInstance,
                worksheet: sheet,
                autoFilterEnabled: true
            }).then(() => {
                exportNext(index + 1); // 다음 그리드 처리
            });
        };

        exportNext(0); // 첫 번째 그리드부터 시작
    },

    /**
     * 클릭한 cell이 edtiable:true면 select한것처럼 보이지 않음 -> setGridClick(grid)로 onCellClick 추가
     * @param grid : grid_id
     */
    setGridClick: (grid) => {
        const _instance = $hs.$(grid)._instance;
        const originalOnCellClick = _instance.option("onCellClick");

        _instance.option("onCellClick", function (e) {
            // 기존 이벤트 먼저 실행
            if (typeof originalOnCellClick === "function") {
                originalOnCellClick(e);
            }

            // 추가 로직 :rowData 강제 선택
            if (e.row && e.row.rowType === "data") {
                _instance.selectRows([e.key], false);
            }
        })
    },

    /**
     * 필터 팝업 재정의 시 사용 (CODE, NAME)
     * @param {any} popup_id : 팝업 ID
     * @param {any} url : 데이터 URL
     * @param {any} param_id : 파라미터 ID
     * @param {any} param : 파라미터
     */
    setFileterPopup: (popup_id, url, param_id, param) => {
        $hs.$(popup_id).popup(grid => {
            grid.set({
                width: "400px",
                height: "300",
                rownumber: true,
                dataurl: url + "?" + param_id + "=" + param,
            });
            grid.columns(col => {
                col.add({
                    label: "CODE", dataField: "CODE", width: 150
                });
                col.add({
                    label: "NAME", dataField: "NAME", width: 150
                });
            })
        });
    },

    /**
     * 필터 팝업 재정의 시 사용 (CODE)
     * @param {any} popup_id : 팝업 ID
     * @param {any} url : 데이터 URL
     * @param {any} param_id : 파라미터 ID
     * @param {any} param : 파라미터
     */
    setFileterPopupCode: (popup_id, url, param_id, param) => {
        $hs.$(popup_id).popup(grid => {
            grid.set({
                width: "400px",
                height: "300",
                rownumber: true,
                dataurl: url + "?" + param_id + "=" + param,
            });
            grid.columns(col => {
                col.add({
                    label: "CODE", dataField: "CODE", width: 150
                });
            })
        });
    }


}



$hs.util.grid = {
    moveup: function (grid) {

        var selectedRows = grid.val();
        if (!selectedRows || selectedRows.length === 0) return;

        var gridData = grid.data();

        // 선택된 행들을 _key 기준으로 정렬 (위에서부터 처리해야 순서 꼬이지 않음)
        selectedRows.sort((a, b) => a["_key"] - b["_key"]);

        var selectedKeys = selectedRows.map(row => row["_key"]);

        for (var i = 0; i < selectedRows.length; i++) {
            var current_uid = selectedRows[i]["_key"];
            var currentIndex = gridData.findIndex(row => row["_key"] === current_uid);

            // 맨 위에 있는 행은 이동 불가
            if (currentIndex === 0) continue;

            var prevRow = gridData[currentIndex - 1];
            var currRow = gridData[currentIndex];

            //Swap
            gridData[currentIndex - 1] = currRow;
            gridData[currentIndex] = prevRow;

            // _key 값도 교환 (정렬 기준이므로 중요)
            var tempKey = currRow["_key"];
            currRow["_key"] = prevRow["_key"];
            prevRow["_key"] = tempKey;

            //선택된 키도 업데이트
            selectedKeys[i] = prevRow["_key"] - 1;
        }
        grid.data(gridData);
        grid.val(selectedKeys);
    }

    ,
    movedown: function (grid) {
       
        var selectedRows = grid.val();
        if (!selectedRows || selectedRows.length === 0) return;

        var gridData = grid.data();

        // 선택된 행들을 _key 기준으로 역순 정렬 (아래로 이동 시 순서 꼬이지 않게)
        selectedRows.sort((a, b) => b["_key"] - a["_key"]);

        var selectedKeys = selectedRows.map(row => row["_key"]);

        for (var i = 0; i < selectedRows.length; i++) {
            var current_uid = selectedRows[i]["_key"];
            var currentIndex = gridData.findIndex(row => row["_key"] === current_uid);

            // 맨 아래에 있는 행은 이동 불가
            if (currentIndex === gridData.length - 1) continue;

            var nextRow = gridData[currentIndex + 1];
            var currRow = gridData[currentIndex];

            // Swap
            gridData[currentIndex + 1] = currRow;
            gridData[currentIndex] = nextRow;

            // _key 값도 교환
            var tempKey = currRow["_key"];
            currRow["_key"] = nextRow["_key"];
            nextRow["_key"] = tempKey;

            // 선택된 키도 업데이트
            selectedKeys[i] = nextRow["_key"] + 1;
        }

        grid.data(gridData);
        grid.val(selectedKeys);
    }

}