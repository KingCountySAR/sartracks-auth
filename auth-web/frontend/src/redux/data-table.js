export const SELECT_MODE_NONE = 0;
export const SELECT_MODE_HIDDEN = 1;
export const SELECT_MODE_SINGLE = 2;
export const SELECT_MODE_MULTI = 3;

export const actionsFactory = (url, tableId, stateLocator) => {
  var self = {

    load: () => (dispatch, getState) => {
      const opts = stateLocator(getState()).opts;
      var addr = url + '?';
      if (opts && typeof opts.size === 'number') addr += `page[size]=${opts.size}&`;
      if (opts && typeof opts.page === 'number') addr += `page[number]=${opts.page}&`;
      if (opts && opts.search) addr += `filter=${encodeURIComponent(opts.search)}&`;
      dispatch({type: 'table/LOAD', table: tableId});
      fetch(addr)
      .then(msg => msg.json())
      .then(json => dispatch({type: 'table/LOADED', table: tableId, payload: json}))
      .catch(err => {
        console.error(err)
        dispatch({type: 'table/LOAD_FAIL', table: tableId, failure: err, data: [], meta: {} })
      })
    },

    setOptions: newOpts => (dispatch, getState) => {
      dispatch({type: 'table/SET_OPTIONS', table: tableId, payload: newOpts })
      dispatch(self.load(dispatch, getState));
    },

    toggleRow: (index, data) => (
      {type: 'table/SELECT', table: tableId, payload: { data, index }}
    )

  };
  return self;
}

function selectRowMulti(selected, payload) {
  const current = selected || []
  const replacement = current.filter(i => payload.index !== i);
  if (replacement.length === current.length) replacement.push(payload.index);
  return replacement
}

function selectRowSingle(selected, payload) {
  const current = selected || []
  return current.indexOf(payload.index) >= 0 ? [] : [payload.index]
}

export const reducerFactory = (storePath) => (state = { opts: { size: 25, page: 1 }, data: [] }, action) => {
  if (action.table !== storePath) return state;

  switch (action.type) {
    case 'table/LOAD':
      return {...state, loading: true}

    case 'table/LOADED':
      return {...state, ...action.payload, loading: false, failed: false, failure: null}

    case 'table/LOAD_FAIL':
      return { ...state, ...action.payload, failed: true, loading: false}

    case 'table/SET_OPTIONS':
      return {...state, opts: { ...state.opts, ...action.payload }, selected: []}

    case 'table/SELECT':
      const selected = state.opts.selectMode === SELECT_MODE_MULTI
                        ? selectRowMulti(state.selected, action.payload)
                        : selectRowSingle(state.selected, action.payload)
      return { ...state, selected }

    default:
      return state
  }
}